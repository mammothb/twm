using Twm.Application.Commands;
using Twm.Application.Config;
using Twm.Application.Diagnostics;
using Twm.Application.Messaging;
using Twm.Application.OutboundPorts;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Coordination;

/// <summary>
/// The platform-neutral coordinator that binds the tested core to an OS. It
/// builds the container tree from the display topology, wires the command bus
/// with every core handler, adopts existing windows on <see cref="Start" />,
/// and reconciles the tree back to the OS after every change. Everything is
/// driven through <see cref="IMonitorSystem" /> and
/// <see cref="IWindowSystem" />, so the whole loop is verifiable on Linux with
/// fakes; the Win32 backends plug in unchanged.
public sealed class WmSession
{
    private readonly IWindowSystem _windows;
    private readonly WindowFilter _filter;
    private readonly Bus _bus;
    private readonly LayoutEngine _layout;
    private readonly Reconciler _reconciler;

    // The window Twm last asked the OS to foreground. SyncFocus ignores the
    // foreground even our own reconcile triggers (so it isn't mistaken for a
    // user click).
    private WindowId? _pendingForeground;

    public WmSession(
        IMonitorSystem monitors,
        IWindowSystem windows,
        Gaps gaps = default,
        WindowFilter? filter = null,
        WorkspaceOptions? workspaces = null,
        int titleBarHeight = LayoutEngine.DefaultTitleBarHeight
    )
    {
        ArgumentNullException.ThrowIfNull(monitors);
        ArgumentNullException.ThrowIfNull(windows);
        _windows = windows;
        _filter = filter ?? new WindowFilter();

        Root = DesktopBuilder.Build(monitors.EnumerateMonitors(), workspaces);
        _layout = new LayoutEngine(gaps, titleBarHeight);
        _bus = new Bus();
        RegisterHandlers();
        _reconciler = new Reconciler(windows);
        _layout.Arrange(Root);
    }

    /// <summary>The live container tree.</summary>
    public RootContainer Root { get; }

    /// <summary>How many windows are currently tiled.</summary>
    public int ManagedWindowCount => Root.Descendants.OfType<TilingWindow>().Count();

    /// <summary>Whether the given window is currently in the tree.</summary>
    public bool IsManaged(WindowId window) => Root.FindWindow(window) is not null;

    /// <summary>
    /// Adopts every currently manageable window, then applies the layout to the
    /// OS.
    /// </summary>
    public void Start()
    {
        foreach (NativeWindowInfo window in _windows.EnumerateWindows())
        {
            if (_filter.IsManageable(window))
            {
                Adopt(window);
            }
        }

        Apply();
    }

    /// <summary>
    /// Adopts a single window if manageable; returns whether it was adopted.
    /// </summary>
    public bool TryAdopt(NativeWindowInfo window)
    {
        ArgumentNullException.ThrowIfNull(window);
        // Skip windows we shouldn't manage, and windows already in the tree
        // (create/show events can fire repeatedly for the same window)
        if (!_filter.IsManageable(window) || Root.FindWindow(window.Id) is not null)
        {
            return false;
        }

        Adopt(window);
        Apply();
        return true;
    }

    /// <summary>
    /// Removes a window from management, e.g., when the OS destroys it, then
    /// reapplies.
    /// </summary>
    public bool Remove(WindowId window)
    {
        // Ignore the constant stream of destroy/hide/cloak events for windows
        // we never managed
        if (Root.FindWindow(window) is null)
        {
            return false;
        }

        _bus.Invoke(new RemoveWindowCommand(window));
        Apply();
        return true;
    }

    /// <summary>
    /// Records that the OS foreground moved to <paramref name="window" />, so
    /// subsequent focus/move commands act from it. No reconcile, the window is
    /// already foreground (the user selected it).
    /// </summary>
    public void SyncFocus(WindowId window)
    {
        TilingWindow? managed = Root.FindWindow(window);
        if (managed is null)
        {
            return;
        }

        if (_pendingForeground is WindowId expected)
        {
            // Consume the foreground event our own reconcile triggered. If it's
            // the window we foregrounded, the tree is already correct, don't
            // treat it as a user click
            _pendingForeground = null;
            if (window == expected)
            {
                Log.Line($"syncfocus 0x{window.Value:X}: ignored (our own foreground)");
                return;
            }
        }

        // Reveal ONLY for a genuine cross-workspace navigation, e.g., taskbar
        // clicking a window on an inactive workspace). A foreground event for a
        // window already on the ACTIVE workspace, including a non-focused tab,
        // must NOT reconcile: cloak/uncloak fires more foreground/cloak
        // events, which feed a foreground->reconcile->foreground storm when
        // cycling tabs (which also starves the exit hotkey). Tabs are switched
        // by keyboard, not by focus events
        bool isOnInactiveWorkspace =
            managed.WorkspaceOf() is Workspace workspace
            && !ReferenceEquals(workspace, managed.MonitorOf()?.LastFocusedChild);

        managed.Focus();

        if (isOnInactiveWorkspace)
        {
            Log.Line($"syncfocus 0x{window.Value:X}: reveal inactive workspace");
            // reconciles (reveals the workspace) and emits LayoutChanged
            Apply();
        }
        else
        {
            Log.Line($"syncfocus 0x{window.Value:X}: focus-only");
            // focus only change (no reconcile): still notify so the bar
            // refreshes the focused title
            _bus.Emit(new LayoutChangedEvent());
        }
    }

    /// <summary>
    /// Handles a hide event (ObjectHide). A window the tree says should be
    /// <b>hidden</b> (an inactive workspace, or a non-focused tab) is one Twm
    /// itself cloaked -> ignored. A window the tree says should be
    /// <b>visible</b> was hidden by the user -> removed from tiling (otherwise
    /// it leaves a ghost tile). Phase 4 tightens this to verify OS state.
    /// Returns whether it was removed.
    /// </summary>
    public bool HandleHidden(WindowId window)
    {
        TilingWindow? target = Root.FindWindow(window);
        if (target is null)
        {
            return false;
        }

        bool removed = target.IsEffectivelyVisible() && Remove(window);
        Log.Line(
            $"hidden 0x{window.Value:X}: {(removed ? "removed (user)" : "ignored (Twm cloak)")}"
        );
        return removed;
    }

    /// <summary>
    /// Handles a minimize event (SystemMinimizeStart). A visible window the
    /// user minimized is removed from tiling (otherwise it leaves a ghost
    /// tile); a window Twm itself hid is ignored. Returns whether it was
    /// removed.
    /// </summary>
    public bool HandleMinimized(WindowId window)
    {
        TilingWindow? target = Root.FindWindow(window);
        if (target is null)
        {
            return false;
        }

        bool removed = target.IsEffectivelyVisible() && Remove(window);
        Log.Line(
            $"minimized 0x{window.Value:X}: {(removed ? "removed (user)" : "ignored (not visible)")}"
        );
        return removed;
    }

    /// <summary>
    /// Handles a cloak event (ObjectCloaked). A cloak is never a user action:
    /// it is Twm's own cloak of a non-visible window, or DWM cascading the
    /// cloak to an owned window (e.g. the Eden Configuration dialog). Never
    /// removes. Returns false.
    /// </summary>
    public bool HandleCloaked(WindowId window)
    {
        if (Root.FindWindow(window) is null)
        {
            return false;
        }

        Log.Line($"cloaked 0x{window.Value:X}: ignored (never remove on cloak)");
        return false;
    }

    /// <summary>
    /// Asks the OS to close the focused window (post WM_CLOSE). Its removal
    /// from the tree arrives later via the destroy WinEvent, not here.
    /// </summary>
    public void CloseFocused()
    {
        if (Root.FocusedWindow() is TilingWindow focused)
        {
            _windows.Close(focused.WindowId);
        }
    }

    /// <summary>
    /// Runs a core command through the bus, then reapplies the layout to the
    /// OS.
    /// </summary>
    public CommandResult Execute(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Log.Line($"execute {command.GetType().Name}");
        CommandResult result = _bus.Invoke(command);
        Apply();
        return result;
    }

    /// <summary>
    /// Uncloaks every managed window so none is left hidden after Twm exits.
    /// Windows on inactive workspaces are cloaked while Twm runs; without this,
    /// quitting would leave them invisible with no WM to restore them. Call on
    /// every exit path.
    /// </summary>
    public void Shutdown()
    {
        foreach (TilingWindow window in Root.Descendants.OfType<TilingWindow>())
        {
            try
            {
                _windows.Show(window.WindowId);
            }
            catch (Exception)
            {
                Log.Line($"shutdown: could not restore 0x{window.WindowId.Value:X}");
            }
        }
    }

    private void Adopt(NativeWindowInfo window)
    {
        Monitor monitor = MonitorRouter.Pick(Root, window.Bounds);
        _bus.Invoke(new AdoptWindowCommand(window.Id, monitor, window.Owner));
    }

    private void Apply()
    {
        _reconciler.Apply(Root);
        _pendingForeground = Root.FocusedWindow()?.WindowId;
        Log.Line(
            $"reconcile: fg=0x{_pendingForeground?.Value ?? 0:X} managed={ManagedWindowCount}"
        );
        _bus.Emit(new LayoutChangedEvent());
    }

    /// <summary>
    /// Subscribes to a WM event, e.g., <see cref="LayoutChangedEvent" /> on the
    /// internal bus, for in-process consumers such as the status bar. Handlers
    /// run on the WM thread.
    /// </summary>
    public void Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IEvent => _bus.Subscribe(handler);

    private void RegisterHandlers()
    {
        _bus.Register(new AdoptWindowHandler(Root, _layout));
        _bus.Register(new FocusInDirectionHandler(Root, _layout));
        _bus.Register(new FocusWorkspaceHandler(Root, _layout));
        _bus.Register(new MoveInDirectionHandler(Root, _layout));
        _bus.Register(new MoveWindowToWorkspaceHandler(Root, _layout));
        _bus.Register(new RemoveWindowHandler(Root, _layout));
        _bus.Register(new ResizeContainerHandler(Root, _layout));
        _bus.Register(new ResizeInDirectionHandler(Root, _layout));
        _bus.Register(new SetLayoutHandler(Root, _layout));
        _bus.Register(new SplitDirectionHandler(Root, _layout));
        _bus.Register(new ToggleSplitDirectionHandler(Root, _layout));
    }
}
