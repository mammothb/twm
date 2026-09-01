using System.Collections.Frozen;
using Twm.Application.Config;
using Twm.Application.OutboundPorts;

namespace Twm.Application.Coordination;

/// <summary>
/// Decides whether an OS window is a "managed window" Twm should tile, or an
/// "ignored window" it must leave alone (taskbars, the desktop, tool palettes,
/// dialogs without a title, etc.). Pure function of the
/// <see cref="NativeWindowInfo" /> snapshot, so it is fully unit-testable on
/// Linux.
///
/// Config <see cref="WindowRule" />s are layered on top of the built-in field
/// predicate: the first matching rule decides (an <c>ignore</c> rule drops a
/// window the defaults would keep; a <c>manage</c> rule rescues one they would
/// drop). With no rules, behavior is exactly the built-in defaults.
/// </summary>
public sealed class WindowFilter(IReadOnlyList<WindowRule>? rules = null)
{
    private readonly IReadOnlyList<WindowRule> _rules = rules ?? [];

    private static readonly FrozenSet<string> s_ignoredClasses = new[]
    {
        "Shell_TrayWnd", // primary taskbar
        "Shell_SecondaryTrayWnd", // taskbar on secondary monitors
        "Progman", // desktop "Program Manager" host
        "WorkerW", // desktop wallpaper worker window
        "Window.UI.Core.CoreWindow", // Start menu, Search, Action Center shells
        "TaskManagerWindow", // Task Manager (elevated)
    }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Whether the given window should be tiled by Twm. Config rules win first
    /// (manage rescues, ignore drops); otherwise built-in field predicate
    /// decides.
    /// </summary>
    public bool IsManageable(NativeWindowInfo window)
    {
        foreach (WindowRule rule in _rules)
        {
            if (rule.Matches(window))
            {
                return rule.Action == WindowRuleAction.Manage;
            }
        }
        return IsManageableByDefaults(window);
    }

    /// <summary>
    /// The built-in predicate: manageable when visible, not cloaked, not
    /// minimized, not a tool or child window, not no-activate/menu-popup, not
    /// elevated, has a non-empty title, and its class is not a known
    /// shell/system window.
    /// </summary>
    private static bool IsManageableByDefaults(NativeWindowInfo window)
    {
        // Hidden or UWP-cloaked (WS_VISIBLE set but not actually on screen).
        if (!window.IsVisible || window.IsCloaked)
        {
            return false;
        }

        // Minimized windows have no meaningful bounds to tile at startup; they
        // get adopted later via show/foreground WinEvent when restored
        if (window.IsMinimized)
        {
            return false;
        }

        // Tool windows are palettes/popups; child windows, e.g., Chromium's
        // "Chrome Legacy Window", are embedded HWNDs, neither is a real
        // application window
        if (window.IsToolWindow || window.IsChild)
        {
            return false;
        }

        // Windows that never take activation (WS_EX_NOACTIVATE) or are owned
        // popups/menus with no title bar (WinUI flyouts like the taskbar's
        // "PopupHost", Notepad++'s autocomplete are not real app windows,
        // adopting them starts a tile-flicker fight with the popup.
        // (glazewm's heuristic: WindowService.IsHandleManageable)
        if (window.IsNoActivate || window.IsMenuPopup)
        {
            return false;
        }

        // Allowlist criterion: a real app window has a title bar (WS_CAPTION)
        // AND the standard raised frame (WS_EX_WINDOWEDGE).
        // Overlays/toolbars/flyouts lack them. Reliable signal for Teams
        // "Sharing Control bar".
        if (!window.HasCaption || !window.HasWindowEdge)
        {
            return false;
        }

        // An elevated window (higher integrity than Twm) can't be repositioned
        // by an unelevated Twm (UIPI); adopting it would leave a ghost tile.
        // Detected on Windows via integrity level; always false in Linux tests.
        if (window.IsElevated)
        {
            return false;
        }

        // Titleless top-level windows are almost always menus/popups spawned
        // by apps
        if (string.IsNullOrWhiteSpace(window.Title))
        {
            return false;
        }

        return !s_ignoredClasses.Contains(window.ClassName);
    }
}
