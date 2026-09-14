using System.Threading;
using Twm.Adapters.Config;
using Twm.Adapters.Ipc;
using Twm.Adapters.Windows;
using Twm.Application.Config;
using Twm.Application.Coordination;
using Twm.Application.Diagnostics;
using Twm.Application.InboundPorts;
using Twm.Application.OutboundPorts;

namespace Twm.App;

/// <summary>
/// Composition root for the WM process. <see cref="Build" /> wires up
/// logging, DPI, systems, config, the session, and per-subsystem hosts.
/// <see cref="Run" /> captures the WM thread id, installs the IPC pipeline
/// (which needs the thread id), and runs the message loop until quit.
/// <see cref="IDisposable.Dispose" /> cascades teardown in reverse order:
/// IPC → events → hotkeys → UI hosts → session → log writer.
/// </summary>
internal sealed class AppHost : IDisposable
{
    private readonly string _configPath;
    private readonly StreamWriter? _logWriter;
    private readonly WindowsWindowSystem _windowSystem;
    private readonly WmSession _session;
    private readonly IReadOnlyDictionary<KeyBinding, KeyEffect> _keymap;
    private readonly HotkeyManager _hotkeyManager;
    private readonly WindowEventRouter _windowEventRouter;
    private readonly StatusBarHost? _statusBar;
    private readonly BorderHost? _border;
    private readonly TabBarHost _tabBar;

    /// <summary>
    /// Named kernel mutex held for the lifetime of the process to block a
    /// second <c>twm</c> from starting. Released (and the handle closed) in
    /// <see cref="Dispose" /> so a second launch can start cleanly after
    /// this one exits.
    /// </summary>
    private readonly Mutex _singleInstanceMutex;

    /// <summary>Set in <see cref="Run" />; null beforehand.</summary>
    private IpcServer? _ipcServer;

    private bool _disposed;

    private AppHost(
        string configPath,
        StreamWriter? logWriter,
        WindowsWindowSystem windowSystem,
        WmSession session,
        IReadOnlyDictionary<KeyBinding, KeyEffect> keymap,
        HotkeyManager hotkeyManager,
        WindowEventRouter windowEventRouter,
        StatusBarHost? statusBar,
        BorderHost? border,
        TabBarHost tabBar,
        Mutex singleInstanceMutex
    )
    {
        _configPath = configPath;
        _logWriter = logWriter;
        _windowSystem = windowSystem;
        _session = session;
        _keymap = keymap;
        _hotkeyManager = hotkeyManager;
        _windowEventRouter = windowEventRouter;
        _statusBar = statusBar;
        _border = border;
        _tabBar = tabBar;
        _singleInstanceMutex = singleInstanceMutex;
    }

    public static AppHost Build(CliArgs args)
    {
        // Acquire the named mutex up front. Whether we got it or not, the
        // handle stays alive on AppHost so the lock holds for the whole
        // process; releasing it during Dispose is what unblocks a future launch.
        Mutex singleInstanceMutex = new(
            initiallyOwned: true,
            "Twm.SingleInstance",
            out bool isOnlyInstance
        );
        if (!isOnlyInstance)
        {
            Console.WriteLine("Twm is already running.");
            Environment.Exit(1);
        }

        StreamWriter? logWriter = null;
        if (args.Log)
        {
            string logPath = Path.Combine(Path.GetDirectoryName(ConfigPaths.Default())!, "twm.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            logWriter = new StreamWriter(logPath, append: false) { AutoFlush = true };
            StreamWriter sink = logWriter;
            Log.Init(line =>
            {
                sink.WriteLine(line);
                Console.WriteLine(line);
            });
            Console.WriteLine($"Logging to {logPath}");
        }

        // Physical-pixel coordinates everywhere. Must precede any monitor/window
        // enumeration.
        WindowsStartup.EnablePerMonitorDpiAwareness();

        var monitorSystem = new WindowsMonitorSystem();
        var windowSystem = new WindowsWindowSystem();

        var configSource = new YamlConfigSource(args.ConfigPath);
        ResolvedConfig config = configSource.Load(monitorSystem.EnumerateMonitors().Count);
        foreach (string configError in config.Errors)
        {
            Console.WriteLine($"config: {configError}");
        }

        var filter = new WindowFilter(config.WindowRules);

        // Let keyboard-driven focus changes actually bring windows to the foreground.
        WindowsStartup.DisableForegroundLockTimeout();

        BarOptions barOptions = config.Bar;

        // Two monitor views: status bar draws at the taskbar edge (raw
        // monitors), tiled windows use the inset WorkArea so they sit clear of
        // the Twm bar.
        IMonitorSystem tilingMonitorSystem = barOptions.Enabled
            ? new InsetMonitorSystem(monitorSystem, barOptions.Height, barOptions.Position)
            : monitorSystem;

        var session = new WmSession(
            monitorSystem: tilingMonitorSystem,
            windowSystem: windowSystem,
            gaps: config.Gaps,
            filter: filter,
            workspaces: config.Workspaces,
            titleBarHeight: config.Tabs.Height
        );

        // Install WinEventHook BEFORE Start(): SetWinEventHook only delivers
        // events that occur after registration. Any window appearing in a
        // Start-after-Install gap would never fire Appeared and stay unmanaged.
        var windowEventRouter = new WindowEventRouter(session, windowSystem);
        windowEventRouter.Install();

        session.Start();

        StatusBarHost? statusBar = barOptions.Enabled
            ? new StatusBarHost(session, monitorSystem, windowSystem, barOptions)
            : null;

        BorderHost? border = config.Border.Enabled ? new BorderHost(session, config.Border) : null;

        var tabBar = new TabBarHost(session, windowSystem, config.Tabs);

        IReadOnlyDictionary<KeyBinding, KeyEffect> keymap = config.Keymap;
        var hotkeyManager = new HotkeyManager();
        foreach (KeyBinding binding in keymap.Keys)
        {
            if (!hotkeyManager.Register(binding))
            {
                Console.WriteLine(
                    $"Warning: could not registery hotkey {binding.Modifiers}+vk0x{binding.VirtualKey:X2} (already in use?)."
                );
            }
        }

        return new AppHost(
            args.ConfigPath,
            logWriter,
            windowSystem,
            session,
            keymap,
            hotkeyManager,
            windowEventRouter,
            statusBar,
            border,
            tabBar,
            singleInstanceMutex
        );
    }

    public int Run()
    {
        uint wmThreadId = MessageLoop.CurrentThreadId();

        Console.CancelKeyPress += (_, cancelArgs) =>
        {
            cancelArgs.Cancel = true;
            MessageLoop.Post(wmThreadId, MessageLoop.WmAppQuit);
        };

        var ipc = new IpcRequestHandler(_session, MessageLoop.Quit, _windowSystem.GetTitle);
        var ipcDispatcher = new WmThreadDispatcher(
            () => MessageLoop.Post(wmThreadId, MessageLoop.WmApp),
            ipc.Handle
        );
        _ipcServer = new IpcServer(ipcDispatcher.DispatchFromBackground);
        _ipcServer.Start();

        Console.WriteLine(
            $"Twm is tiling {_session.ManagedWindowCount} window(s) with {_keymap.Count} keybinding(s). Config: {_configPath}. Default exit: Alt+Shift+E."
        );

        var messageRouter = new WmMessageRouter(
            _session,
            _windowSystem,
            _hotkeyManager,
            _keymap,
            ipcDispatcher,
            _statusBar
        );

        MessageLoop.Run(messageRouter.Handle);
        return 0;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        // Stop reacting to the OS first so no window is left hidden after Twm exists.
        _ipcServer?.Dispose();
        _windowEventRouter.Dispose();
        _hotkeyManager.UnregisterAll();
        _statusBar?.Dispose();
        _border?.Dispose();
        _tabBar.Dispose();
        _session.Shutdown();
        _logWriter?.Dispose();
        // Release the single-instance lock last, after every Twm-owned window
        // and resource is gone. Dispose() releases if still owned, then closes
        // the handle.
        _singleInstanceMutex.Dispose();
    }
}
