using System.Runtime.InteropServices;
using Twm.Domain.Tree;

namespace Twm.Adapters.Windows;

/// <summary>
/// The kind of OS window event, normalized from the raw Win32 WinEvent
/// constant.
/// </summary>
public enum WindowEventKind
{
    /// <summary>
    /// A window was created, shown, uncloaded, or un-minimized, a candidate to
    /// adopt.
    /// </summary>
    Appeared,

    /// <summary>
    /// A window was destroyed, always remove it from management.
    /// </summary>
    Destroyed,

    /// <summary>
    /// A window was hidden (ObjectHide), remove only if the user did it.
    /// </summary>
    Hidden,

    /// <summary>
    /// A window was minimized (SystemMinimizeStart). A genuine user minimize
    /// should remove the window from tiling.
    /// </summary>
    Minimized,

    /// <summary>
    /// A window was cloaked (ObjectCloaked) — Twm's own cloak or the DWM
    /// cascade to owned windows; never a user action.
    /// </summary>
    Cloaked,

    /// <summary>
    /// The foreground window changed, sync tree focus to it.
    /// </summary>
    Foreground,
}

/// <summary>
/// Subscribes to WinEvents (OUTOFCONTEXT, so callbacks fire on the pump thread)
/// for window create/destroy/show/hide and foreground changes, and forwards
/// them as <see cref="WindowEventKind" /> + <see cref="WindowId" />.
/// Single-instance: the callback dispatches through a static handler, since
/// SetWinEventHook has no user-data parameter.
/// </summary>
public sealed unsafe partial class WinEventHook : IDisposable
{
    private static WinEventHook? s_owner;
    private static Action<WindowEventKind, WindowId>? s_handler;

    private readonly List<nint> _hooks = [];

    [LibraryImport("user32.dll")]
    private static partial nint SetWinEventHook(
        WinEvent eventMin,
        WinEvent eventMax,
        nint hmodWinEventProc,
        delegate* unmanaged<nint, uint, nint, int, int, uint, uint, void> pfnWinEventProc,
        uint idProcess,
        uint idThread,
        WinEventFlags dwFlags
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnhookWinEvent(nint hWinEventHook);

    [UnmanagedCallersOnly]
    private static void OnWinEvent(
        nint hook,
        uint eventType,
        nint hWnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint eventTime
    )
    {
        // Only top-level window event (OBJID_WINDOW == 0, CHILDID_SELF == 0;
        // skip control/menus
        if (
            idObject != 0
            || idChild != 0
            || hWnd == 0
            || s_handler is not Action<WindowEventKind, WindowId> handler
        )
        {
            return;
        }

        // Cloak/minimize makes a managed window vanish without a destroy/hide;
        // uncloadk and minimize-end bring it back. Handling them presents
        // ghost tiles (empty slots)
        WindowEventKind? kind = (WinEvent)eventType switch
        {
            WinEvent.ObjectCreate
            or WinEvent.ObjectShow
            or WinEvent.ObjectUncloaked
            or WinEvent.SystemMinimizeEnd => WindowEventKind.Appeared,
            WinEvent.ObjectDestroy => WindowEventKind.Destroyed,
            WinEvent.ObjectHide => WindowEventKind.Hidden,
            WinEvent.ObjectCloaked => WindowEventKind.Cloaked,
            WinEvent.SystemMinimizeStart => WindowEventKind.Minimized,
            WinEvent.SystemForeground => WindowEventKind.Foreground,
            _ => null,
        };

        if (kind is WindowEventKind value)
        {
            handler(value, new WindowId(hWnd));
        }
    }

    /// <summary>
    /// Installs the hooks and routes events to <paramref name="handler" /> until
    /// disposed.
    /// </summary>
    public void Install(Action<WindowEventKind, WindowId> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        // The WinEvent callback is static, so the handler is process-wide, only
        // one hook set may be installed at a time
        if (s_owner is not null)
        {
            throw new InvalidOperationException("A WinEventHook is already installed");
        }
        s_owner = this;
        s_handler = handler;

        const WinEventFlags flags = WinEventFlags.OutOfContext | WinEventFlags.SkipOwnProcess;

        // Four tight ranges rather than one wide one, so we never receive the
        // very frequent EVENT_OBJECT_LOCATIONCHANGE (0x800B) that sits between
        // these object events
        _hooks.Add(
            SetWinEventHook(
                WinEvent.SystemForeground,
                WinEvent.SystemForeground,
                0,
                &OnWinEvent,
                0,
                0,
                flags
            )
        );
        _hooks.Add(
            SetWinEventHook(
                WinEvent.SystemMinimizeStart,
                WinEvent.SystemMinimizeEnd,
                0,
                &OnWinEvent,
                0,
                0,
                flags
            )
        );
        _hooks.Add(
            SetWinEventHook(WinEvent.ObjectCreate, WinEvent.ObjectHide, 0, &OnWinEvent, 0, 0, flags)
        );
        _hooks.Add(
            SetWinEventHook(
                WinEvent.ObjectCloaked,
                WinEvent.ObjectUncloaked,
                0,
                &OnWinEvent,
                0,
                0,
                flags
            )
        );
    }

    public void Dispose()
    {
        foreach (nint hook in _hooks)
        {
            if (hook != 0)
            {
                UnhookWinEvent(hook);
            }
        }

        _hooks.Clear();
        // Only the installing instance clears the shared handler/owner, so a
        // stray Dispose on a non-owner can't disable live hooks
        if (ReferenceEquals(s_owner, this))
        {
            s_owner = null;
            s_handler = null;
        }
    }

    /// <summary>WinEvent constants this hook subscribes to.</summary>
    private enum WinEvent : uint
    {
        SystemForeground = 0x0003, // EVENT_SYSTEM_FOREGROUND
        SystemMinimizeStart = 0x0016, // EVENT_SYSTEM_MINIMIZESTART
        SystemMinimizeEnd = 0x0017, // EVENT_SYSTEM_MINIMIZEEND
        ObjectCreate = 0x8000, // EVENT_OBJECT_CREATE
        ObjectDestroy = 0x8001, // EVENT_OBJECT_DESTROY
        ObjectShow = 0x8002, // EVENT_OBJECT_SHOW
        ObjectHide = 0x8003, // EVENT_OBJECT_HIDE
        ObjectCloaked = 0x8017, // EVENT_OBJECT_CLOAKED
        ObjectUncloaked = 0x8018, // EVENT_OBJECT_UNCLOAKED
    }

    /// <summary>SetWinEventHook flags.</summary>
    [Flags]
    private enum WinEventFlags : uint
    {
        OutOfContext = 0x0000, // WINEVENT_OUTOFCONTEXT
        SkipOwnProcess = 0x0002, // WINEVENT_SKIPOWNPROCESS
    }
}
