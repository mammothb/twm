using System.Runtime.InteropServices;
using System.Threading.Channels;
using Twm.Config;
using Twm.Interop;
using Twm.Layout;

namespace Twm.Core;

/// <summary>
/// Central state machine. Owns the window table and per-monitor trees.
/// All mutation happens on the main async loop; the native pump thread
/// only enqueues messages.
/// </summary>
public sealed class WindowManager(TwmConfig config)
{
    private const int ResizeStepPx = 60;

    private readonly record struct NativeMessage(
        uint EventId,
        nint Hwnd,
        Modifiers Mods,
        uint VKey,
        bool IsHotkey
    );

    private sealed class MonitorState
    {
        public required nint Handle { get; init; }
        public required Rect WorkArea { get; init; }
        public SplitTree Tree { get; } = new();
    }

    private readonly uint _ownPid = (uint)Environment.ProcessId;
    private readonly TwmConfig _config = config;
    private readonly Channel<NativeMessage> _queue = Channel.CreateUnbounded<NativeMessage>(
        new UnboundedChannelOptions { SingleReader = true }
    );

    private readonly Dictionary<nint, ManagedWindow> _windows = [];
    private readonly Dictionary<nint, MonitorState> _monitors = [];
    private readonly Dictionary<nint, DateTime> _lastSnapAttempt = [];
    private volatile bool _quit;
    private nint _draggingHwnd;

    // ---------------------------------------------------------------
    // Pump-thread callbacks — enqueue only, never block.
    // ---------------------------------------------------------------

    /// <summary>Called from the keyboard hook. True = swallow the key.</summary>
    public bool MatchHotkey(Modifiers mods, uint vKey)
    {
        if (!_config.Bindings.ContainsKey(new KeyCombo(mods, vKey)))
        {
            return false;
        }

        _queue.Writer.TryWrite(new NativeMessage(0, 0, mods, vKey, IsHotkey: true));
        return true;
    }

    /// <summary>Called from the winevent hook.</summary>
    public void EnqueueWinEvent(uint eventId, nint hwnd) =>
        _queue.Writer.TryWrite(new NativeMessage(eventId, hwnd, Mods: 0, VKey: 0, IsHotkey: false));

    // ---------------------------------------------------------------
    // Startup
    // ---------------------------------------------------------------

    public void ScanExisting()
    {
        EnumerateMonitors();
        User32.EnumWindows(
            (hwnd, _) =>
            {
                TryTrack(hwnd, focusNew: false);
                return true;
            },
            0
        );
        Log.Info($"Managing {_windows.Count} window(s) across {_monitors.Count} monitor(s).");
    }

    private void EnumerateMonitors() =>
        User32.EnumDisplayMonitors(
            0,
            0,
            (nint hMonitor, nint _, ref User32.RECT _, nint _) =>
            {
                EnsureMonitor(hMonitor);
                return true;
            },
            0
        );

    private MonitorState? EnsureMonitor(nint hMonitor)
    {
        if (_monitors.TryGetValue(hMonitor, out MonitorState? existing))
        {
            return existing;
        }

        var info = new User32.MONITORINFO { CbSize = (uint)Marshal.SizeOf<User32.MONITORINFO>() };
        if (!User32.GetMonitorInfo(hMonitor, ref info))
        {
            return null;
        }

        var state = new MonitorState
        {
            Handle = hMonitor,
            WorkArea = Rect.FromLtrb(
                info.RcWork.Left,
                info.RcWork.Top,
                info.RcWork.Right,
                info.RcWork.Bottom
            ),
        };
        _monitors[hMonitor] = state;
        Log.Info($"Monitor {hMonitor}: work area {state.WorkArea}");
        return state;
    }

    // ---------------------------------------------------------------
    // Main event loop
    // ---------------------------------------------------------------

    public async Task<int> RunAsync()
    {
        ChannelReader<NativeMessage> reader = _queue.Reader;
        while (!_quit && await reader.WaitToReadAsync())
        {
            while (!_quit && reader.TryRead(out NativeMessage message))
            {
                if (message.IsHotkey)
                {
                    HandleHotkey(message);
                }
                else
                {
                    HandleWinEvent(message.EventId, message.Hwnd);
                }
            }
        }

        return 0;
    }

    private void HandleHotkey(NativeMessage message)
    {
        if (
            _config.Bindings.TryGetValue(
                new KeyCombo(message.Mods, message.VKey),
                out CommandKind command
            )
        )
        {
            Execute(command);
        }
    }

    private void Execute(CommandKind command)
    {
        MonitorState? state = ActiveState();

        switch (command)
        {
            case CommandKind.FocusLeft:
                FocusDirection(state, Direction.Left);
                break;
            case CommandKind.FocusRight:
                FocusDirection(state, Direction.Right);
                break;
            case CommandKind.FocusUp:
                FocusDirection(state, Direction.Up);
                break;
            case CommandKind.FocusDown:
                FocusDirection(state, Direction.Down);
                break;

            case CommandKind.MoveLeft:
                MoveWindow(state, Direction.Left);
                break;
            case CommandKind.MoveRight:
                MoveWindow(state, Direction.Right);
                break;
            case CommandKind.MoveUp:
                MoveWindow(state, Direction.Up);
                break;
            case CommandKind.MoveDown:
                MoveWindow(state, Direction.Down);
                break;

            case CommandKind.ResizeLeft:
                ResizeWindow(state, Direction.Left);
                break;
            case CommandKind.ResizeRight:
                ResizeWindow(state, Direction.Right);
                break;
            case CommandKind.ResizeUp:
                ResizeWindow(state, Direction.Up);
                break;
            case CommandKind.ResizeDown:
                ResizeWindow(state, Direction.Down);
                break;

            case CommandKind.ToggleSplitOrientation:
                if (state?.Tree.ToggleOrientation() == true)
                {
                    ApplyLayout(state);
                }

                break;

            case CommandKind.CloseFocusedWindow:
                if (state?.Tree.Focused is { } focused)
                {
                    User32.PostMessage(focused.Hwnd, User32.WM_CLOSE, 0, 0);
                }

                break;

            case CommandKind.QuitTwm:
                Log.Info("Quitting (quit_twm binding).");
                _quit = true;
                break;
        }
    }

    private void FocusDirection(MonitorState? state, Direction direction)
    {
        if (state?.Tree.FocusDirection(direction) != true)
        {
            return;
        }

        if (state.Tree.Focused is { } leaf)
        {
            NativeWindow.Focus(leaf.Hwnd);
        }
    }

    private void MoveWindow(MonitorState? state, Direction direction)
    {
        if (state?.Tree.MoveFocused(direction) != true)
        {
            return;
        }

        ApplyLayout(state);
    }

    private void ResizeWindow(MonitorState? state, Direction direction)
    {
        if (state?.Tree.ResizeFocused(direction, ResizeStepPx) != true)
        {
            return;
        }

        ApplyLayout(state);
    }

    private MonitorState? ActiveState()
    {
        nint foreground = User32.GetForegroundWindow();
        if (
            foreground != nint.Zero
            && _windows.TryGetValue(foreground, out ManagedWindow? win)
            && _monitors.TryGetValue(win.Monitor, out MonitorState? byForeground)
        )
        {
            return byForeground;
        }

        return _monitors.Values.FirstOrDefault(m => m.Tree.Focused is not null)
            ?? _monitors.Values.FirstOrDefault(m => m.Tree.Count > 0);
    }

    // ---------------------------------------------------------------
    // Winevent handling
    // ---------------------------------------------------------------

    private void HandleWinEvent(uint eventId, nint hwnd)
    {
        switch (eventId)
        {
            case WinEvent.SystemForeground:
                SyncFocus(hwnd);
                break;

            case WinEvent.ObjectShow:
            case WinEvent.ObjectUncloaked:
                TryTrack(hwnd, focusNew: true);
                break;

            case WinEvent.ObjectDestroy:
            case WinEvent.ObjectHide:
            case WinEvent.ObjectCloaked:
                Untrack(hwnd);
                break;

            case WinEvent.ObjectLocationChange:
                // Fires constantly for every window in the system — bail fast.
                if (_windows.ContainsKey(hwnd) && hwnd != _draggingHwnd)
                {
                    SnapBack(hwnd, "moved off its tile");
                }

                break;

            case WinEvent.SystemMoveSizeStart:
                if (_windows.ContainsKey(hwnd))
                {
                    _draggingHwnd = hwnd;
                    Log.Trace($"drag started: {hwnd}");
                }
                break;

            case WinEvent.SystemMoveSizeEnd:
                if (_draggingHwnd == hwnd)
                {
                    _draggingHwnd = 0;
                    SnapBack(hwnd, "drag ended");
                }
                break;
        }
    }

    private void SyncFocus(nint hwnd)
    {
        if (!_windows.TryGetValue(hwnd, out ManagedWindow? win))
        {
            return;
        }

        MonitorState? state = _monitors.GetValueOrDefault(win.Monitor);
        state?.Tree.SetFocused(win.Leaf);
    }

    private void TryTrack(nint hwnd, bool focusNew)
    {
        if (_windows.ContainsKey(hwnd) || !User32.IsWindow(hwnd))
        {
            return;
        }

        if (!NativeWindow.IsEligible(hwnd, _ownPid, out string reason))
        {
            Log.Trace($"skip 0x{hwnd:X} [{NativeWindow.ClassName(hwnd)}]: {reason}");
            return;
        }

        nint hMonitor = NativeWindow.MonitorOf(hwnd);
        MonitorState? state = EnsureMonitor(hMonitor);
        if (state is null)
        {
            return;
        }

        RestoreIfMaximized(hwnd);

        WindowLeaf leaf = state.Tree.Insert(hwnd);
        var window = new ManagedWindow
        {
            Hwnd = hwnd,
            Title = NativeWindow.Title(hwnd),
            ProcessName = NativeWindow.ProcessName(hwnd),
            Monitor = hMonitor,
            Leaf = leaf,
        };
        _windows[hwnd] = window;
        Log.Info($"tiled '{window.Title}' [{window.ProcessName}]");

        ApplyLayout(state);
        if (focusNew)
        {
            NativeWindow.Focus(hwnd);
        }
    }

    private void Untrack(nint hwnd)
    {
        if (!_windows.Remove(hwnd, out ManagedWindow? window))
        {
            return;
        }

        _lastSnapAttempt.Remove(hwnd);
        if (hwnd == _draggingHwnd)
        {
            _draggingHwnd = 0;
        }

        MonitorState? state = _monitors.GetValueOrDefault(window.Monitor);
        if (state is null)
        {
            return;
        }

        bool wasFocused = ReferenceEquals(state.Tree.Focused, window.Leaf);
        state.Tree.Remove(window.Leaf);
        Log.Info($"released '{window.Title}' [{window.ProcessName}]");

        if (state.Tree.Count > 0)
        {
            ApplyLayout(state);
        }

        if (wasFocused && state.Tree.Focused is { } next)
        {
            NativeWindow.Focus(next.Hwnd);
        }
    }

    // ---------------------------------------------------------------
    // Geometry
    // ---------------------------------------------------------------

    private void ApplyLayout(MonitorState state)
    {
        if (state.Tree.Root is null || state.Tree.Count == 0)
        {
            return;
        }

        state.Tree.Apply(state.WorkArea);

        foreach (WindowLeaf leaf in state.Tree.Leaves())
        {
            if (!_windows.ContainsKey(leaf.Hwnd))
            {
                continue; // defensive: tree/table desync
            }

            if (User32.IsIconic(leaf.Hwnd))
            {
                continue; // positioning minimized windows misbehaves
            }

            NativeWindow.SetTileRect(leaf.Hwnd, leaf.AssignedRect);
        }
    }

    /// <summary>
    /// Forces a managed window back onto its tile. The equality check
    /// makes our own SetWindowPos echoes no-ops; the debounce prevents
    /// fighting apps that refuse to resize.
    /// </summary>
    private void SnapBack(nint hwnd, string cause)
    {
        if (!_windows.TryGetValue(hwnd, out ManagedWindow? window))
        {
            return;
        }

        if (User32.IsIconic(hwnd))
        {
            return;
        }

        MonitorState? state = _monitors.GetValueOrDefault(window.Monitor);
        if (state is null || state.Tree.Root is null)
        {
            return;
        }

        Rect target = window.Leaf.AssignedRect;
        if (target.IsEmpty)
        {
            return;
        }

        Rect current = DwmApi.GetExtendedFrameBounds(hwnd);
        if (current == target)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        if (
            _lastSnapAttempt.TryGetValue(hwnd, out DateTime last)
            && (now - last).TotalMilliseconds < 500
        )
        {
            return;
        }

        _lastSnapAttempt[hwnd] = now;

        Log.Trace($"snap back ({cause}): {window.ProcessName}");
        RestoreIfMaximized(hwnd);
        NativeWindow.SetTileRect(hwnd, target);
    }

    private static void RestoreIfMaximized(nint hwnd)
    {
        var placement = new User32.WINDOWPLACEMENT
        {
            Length = (uint)Marshal.SizeOf<User32.WINDOWPLACEMENT>(),
        };
        if (
            User32.GetWindowPlacement(hwnd, ref placement)
            && placement.ShowCmd == User32.SW_SHOWMAXIMIZED
        )
        {
            User32.ShowWindow(hwnd, User32.SW_RESTORE);
        }
    }
}
