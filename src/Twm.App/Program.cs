using System.Threading;
using Twm.Adapters.Config;
using Twm.Adapters.Ipc;
using Twm.Adapters.Windows;
using Twm.App;
using Twm.Application.Config;
using Twm.Application.Coordination;
using Twm.Application.Diagnostics;
using Twm.Application.InboundPorts;
using Twm.Application.OutboundPorts;
using Twm.Domain.Tree;
using Twm.Presentation;

bool dump = args.Contains("--dump");
bool cloakTest = args.Contains("--cloak-test");
bool cloakProbe = args.Contains("--cloak-probe");
bool useConsole = args.Contains("--console");

if (!OperatingSystem.IsWindows())
{
    Console.WriteLine("Twm runs on Windows only.");
    return 1;
}

if (dump || cloakTest || cloakProbe)
{
    WindowsStartup.AttachParentConsole();
}
else if (useConsole)
{
    WindowsStartup.AttachParentConsole();
    WindowsStartup.AllocateConsole();
}

StreamWriter? logWriter = null;
if (args.Contains("--log"))
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

// Physical-pixel coordinates everywhere. Must preceed any monitor/window
// enumeration
WindowsStartup.EnablePerMonitorDpiAwareness();

var monitors = new WindowsMonitorSystem();
var windows = new WindowsWindowSystem();

int configIndex = Array.IndexOf(args, "--config");
string configPath =
    0 <= configIndex && configIndex < args.Length - 1
        ? args[configIndex + 1]
        : ConfigPaths.Default();

var configSource = new YamlConfigSource(configPath);
ResolvedConfig config = configSource.Load(monitors.EnumerateMonitors().Count);
foreach (string configError in config.Errors)
{
    Console.WriteLine($"config: {configError}");
}

var filter = new WindowFilter(config.WindowRules);

if (dump)
{
    return DiagnosticModes.Dump(monitors, windows, filter);
}

if (cloakTest)
{
    return DiagnosticModes.CloakTest(windows, filter);
}

if (cloakProbe)
{
    int probeIndex = Array.IndexOf(args, "--cloak-probe");
    if (probeIndex >= 0 && probeIndex < args.Length - 1)
    {
        string probeText = args[probeIndex + 1];
        if (probeText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            probeText = probeText[2..];
        }
        if (
            nint.TryParse(
                probeText,
                System.Globalization.NumberStyles.HexNumber,
                null,
                out nint probeHwnd
            )
        )
        {
            return DiagnosticModes.CloakProbe(windows, probeHwnd);
        }
    }
    Console.WriteLine("--cloak-probe needs a hex HWND, e.g. --cloak-probe 0x1234.");
    return 1;
}

using var mutex = new Mutex(initiallyOwned: true, "Twm.SingleInstance", out bool isOnlyInstance);
if (!isOnlyInstance)
{
    Console.WriteLine("Twm is already running.");
    return 1;
}

// Let keyboard-driven focus changes actually bring windows to the foreground
WindowsStartup.DisableForegroundLockTimeout();

BarOptions barOptions = config.Bar;
IMonitorSystem tilingMonitors = barOptions.Enabled
    ? new InsetMonitorSystem(monitors, barOptions.Height, barOptions.Position)
    : monitors;

var session = new WmSession(
    monitors: tilingMonitors,
    windows: windows,
    gaps: config.Gaps,
    filter: filter,
    workspaces: config.Workspaces,
    titleBarHeight: config.Tabs.Height
);
session.Start();

StatusBarManager? statusBar = null;
string lastBarClock = "";
nuint clockTimer = 0;
if (barOptions.Enabled)
{
    statusBar = new StatusBarManager(
        [.. DesktopBuilder.OrderPrimaryFirst(monitors.EnumerateMonitors())],
        barOptions
    );
    RefreshBars();
    session.Subscribe<LayoutChangedEvent>(_ => RefreshBars());
    clockTimer = MessageLoop.StartTimer(1000);
}

BorderOptions borderOptions = config.Border;
BorderWindow? border = null;
if (borderOptions.Enabled)
{
    border = new BorderWindow(borderOptions.Color, borderOptions.Width);
    UpdateBorder();
    session.Subscribe<LayoutChangedEvent>(_ => UpdateBorder());
}

var tabBar = new TabBarManager(
    config.Tabs.Background,
    config.Tabs.Foreground,
    config.Tabs.ActiveBackground,
    config.Tabs.Height
);
RefreshTabBars();
session.Subscribe<LayoutChangedEvent>(_ => RefreshTabBars());

IReadOnlyDictionary<KeyBinding, KeyEffect> keymap = config.Keymap;
var hotkey = new HotkeyManager();
foreach (KeyBinding binding in keymap.Keys)
{
    if (!hotkey.Register(binding))
    {
        Console.WriteLine(
            $"Warning: could not registery hotkey {binding.Modifiers}+vk0x{binding.VirtualKey:X2} (already in use?)."
        );
    }
}

var hook = new WinEventHook();
hook.Install(
    (kind, id) =>
    {
        Log.Line($"winevent {kind} 0x{id.Value:X}");
        switch (kind)
        {
            case WindowEventKind.Appeared:
                if (!session.IsManaged(id) && session.TryAdopt(windows.Describe(id)))
                {
                    Console.WriteLine(
                        $"managed - {windows.GetTitle(id)} ({session.ManagedWindowCount} tiled)"
                    );
                }
                break;
            case WindowEventKind.Destroyed:
                if (session.Remove(id))
                {
                    Console.WriteLine($"unmanaged ({session.ManagedWindowCount} tiled)");
                }
                break;
            case WindowEventKind.Hidden:
                if (session.HandleHidden(id))
                {
                    Console.WriteLine($"unmanaged ({session.ManagedWindowCount} tiled)");
                }
                break;
            case WindowEventKind.Foreground:
                session.SyncFocus(id);
                break;
        }
    }
);

Console.WriteLine(
    $"Twm is tiling {session.ManagedWindowCount} window(s) with {keymap.Count} keybinding(s). Config: {configPath}. Default exit: Alt+Shift+E."
);

uint wmThreadId = MessageLoop.CurrentThreadId();

Console.CancelKeyPress += (_, cancelArgs) =>
{
    cancelArgs.Cancel = true;
    MessageLoop.Post(wmThreadId, MessageLoop.WmAppQuit);
};

var ipc = new IpcRequestHandler(session, MessageLoop.Quit, windows.GetTitle);
var ipcDispatcher = new WmThreadDispatcher(
    () => MessageLoop.Post(wmThreadId, MessageLoop.WmApp),
    ipc.Handle
);
var ipcServer = new IpcServer(ipcDispatcher.DispatchFromBackground);
ipcServer.Start();

MessageLoop.Run(
    (message, wParam, _) =>
    {
        if (message == MessageLoop.WmApp)
        {
            ipcDispatcher.Drain();
            return;
        }
        if (message == MessageLoop.WmAppQuit)
        {
            MessageLoop.Quit();
            return;
        }
        if (message == MessageLoop.WmTimer)
        {
            if (statusBar is not null && BarViewModel.Clock(DateTimeOffset.Now) != lastBarClock)
            {
                RefreshBars();
            }
            return;
        }

        if (
            !hotkey.TryResolve(message, wParam, out KeyBinding binding)
            || !keymap.TryGetValue(binding, out KeyEffect? effect)
        )
        {
            return;
        }

        Log.Line(
            $"hotkey {binding.Modifiers}+0x{binding.VirtualKey:X2} -> {effect.GetType().Name}"
        );
        switch (effect)
        {
            case RunCommand run:
                session.Execute(run.Command);
                if (session.Root.FocusedWindow() is TilingWindow focused)
                {
                    Console.WriteLine($"focus - {windows.GetTitle(focused.WindowId)}");
                }
                break;
            case CloseFocusedWindow:
                session.CloseFocused();
                break;
            case ExitWm:
                MessageLoop.Quit();
                break;
        }
    }
);

// Teardown, stop reacting to the OS first then uncloak everything so no window
// is left hidden after Twm exists
ipcServer.Dispose();
hook.Dispose();
hotkey.UnregisterAll();
if (statusBar is not null)
{
    MessageLoop.StopTimer(clockTimer);
    statusBar.Dispose();
}

if (border is not null)
{
    border.Dispose();
    BorderWindow.UnregisterSharedClass();
}

tabBar.Dispose();
logWriter?.Dispose();

session.Shutdown();
return 0;

// Rebuilds the bar snapshot from the current tree and repaints.
void RefreshBars()
{
    if (statusBar is null)
    {
        return;
    }

    BarSnapshot snapshot = BarViewModel.Build(session.Root, windows.GetTitle, DateTimeOffset.Now);
    statusBar.Update(snapshot);
    lastBarClock = snapshot.Clock;
}

// Rebuilds and repaints the tab/stack bars from the current tree.
void RefreshTabBars() => tabBar.Update(TabBarViewModel.Build(session.Root, windows.GetTitle));

void UpdateBorder()
{
    if (border is null)
    {
        return;
    }

    if (session.Root.FocusedWindow() is TilingWindow focused)
    {
        border.MoveTo(focused.Bounds);
    }
    else
    {
        border.Hide();
    }
}
