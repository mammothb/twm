using System.Runtime.InteropServices;
using Twm.Interop;
using Twm.Layout;

namespace Twm.Core;

/// <summary>
/// Stateless queries and mutations on raw HWNDs: identity info,
/// eligibility filtering, DPI-correct geometry, focus.
/// </summary>
public static class NativeWindow
{
    // Known non-manageable top-level windows (shell chrome, popups).
    private static readonly HashSet<string> s_classDenylist = new(StringComparer.OrdinalIgnoreCase)
    {
        "Progman", // desktop
        "WorkerW", // desktop wallpaper host
        "Shell_TrayWnd", // taskbar
        "Shell_SecondaryTrayWnd", // secondary taskbars
        "NotifyIconOverflowWindow", // tray overflow flyout
        "SysListView32", // desktop icon list / tray lists
        "SysPager",
        "ToolbarWindow32",
        "Windows.UI.Core.CoreWindow", // Start menu, search, UWP islands
        "XamlExplorerHostIslandWindow",
        "TopLevelWindowForOverflowXamlIsland",
        "TaskListThumbnailWnd", // taskbar thumbnails
        "TaskbarThumbnailWnd",
        "MsgrIMEWindowClass", // IME ghosts
        "SysShadow", // menu shadows
        "Tooltips_class32",
        "ClockFlyoutWindow",
        "NativeHWNDHost",
    };

    private const int GwlExStyle = -20;

    /// <summary>
    /// Decides whether a top-level window should be tiled. Cheap checks
    /// first; the expensive elevation probe runs last.
    /// </summary>
    public static bool IsEligible(nint hwnd, uint ownPid, out string reason)
    {
        if (!User32.IsWindow(hwnd))
        {
            return Fail("destroyed", out reason);
        }

        if (!User32.IsWindowVisible(hwnd))
        {
            return Fail("invisible", out reason);
        }

        if (
            User32.GetAncestor(
                hwnd,
                2 /* GA_ROOT */
            ) != hwnd
        )
        {
            return Fail("child-window", out reason);
        }

        uint pid = User32.GetWindowThreadProcessId(hwnd, out _);
        if (pid == ownPid)
        {
            return Fail("own-process", out reason);
        }

        long exStyle = User32.GetWindowStyle(hwnd, GwlExStyle);
        if ((exStyle & User32.WS_EX_TOOLWINDOW) != 0)
        {
            return Fail("toolwindow", out reason);
        }

        if (Title(hwnd).Length == 0)
        {
            return Fail("untitled", out reason);
        }

        string className = ClassName(hwnd);
        if (s_classDenylist.Contains(className))
        {
            return Fail($"denylisted:{className}", out reason);
        }

        if (DwmApi.IsCloaked(hwnd))
        {
            return Fail("cloaked", out reason);
        }

        if (Kernel32.IsProcessElevated(pid))
        {
            return Fail("elevated-owner", out reason);
        }

        reason = "";
        return true;
    }

    private static bool Fail(string why, out string reason)
    {
        reason = why;
        return false;
    }

    public static string Title(nint hwnd)
    {
        int length = User32.GetWindowTextLength(hwnd);
        if (length <= 0)
        {
            return "";
        }

        char[] buffer = new char[length + 1];
        int copied = User32.GetWindowText(hwnd, buffer, buffer.Length);
        return new string(buffer, 0, Math.Min(copied, length));
    }

    public static string ClassName(nint hwnd)
    {
        char[] buffer = new char[256];
        int copied = User32.GetClassName(hwnd, buffer, buffer.Length);
        return new string(buffer, 0, Math.Max(0, copied));
    }

    public static string ProcessName(nint hwnd) => ProcessNames.OfWindow(hwnd);

    /// <summary>
    /// Positions a window so that its *visible* frame (extended frame
    /// bounds) lands exactly on <paramref name="targetFrame"/>, hiding the
    /// invisible resize borders from the layout math.
    /// </summary>
    public static void SetTileRect(nint hwnd, Rect targetFrame)
    {
        Rect currentFrame = DwmApi.GetExtendedFrameBounds(hwnd);
        User32.GetWindowRect(hwnd, out User32.RECT outer);

        int offsetX = currentFrame.X - outer.Left;
        int offsetY = currentFrame.Y - outer.Top;
        int extraWidth = outer.Right - outer.Left - currentFrame.Width;
        int extraHeight = outer.Bottom - outer.Top - currentFrame.Height;

        User32.SetWindowPos(
            hwnd,
            0,
            targetFrame.X - offsetX,
            targetFrame.Y - offsetY,
            targetFrame.Width + extraWidth,
            targetFrame.Height + extraHeight,
            User32.SWP_NOZORDER | User32.SWP_NOACTIVATE
        );
    }

    /// <summary>
    /// Focus a window despite running in the background:
    /// attach our input queue to the foreground thread first.
    /// </summary>
    public static void Focus(nint hwnd)
    {
        nint foreground = User32.GetForegroundWindow();
        if (foreground == hwnd)
        {
            return;
        }

        uint foregroundThread =
            foreground != nint.Zero ? User32.GetWindowThreadProcessId(foreground, out _) : 0;
        uint myThread = Kernel32.GetCurrentThreadId();

        bool attached = false;
        try
        {
            if (foregroundThread != 0 && foregroundThread != myThread)
            {
                attached = User32.AttachThreadInput(myThread, foregroundThread, true);
            }

            _ = User32.SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attached)
            {
                User32.AttachThreadInput(myThread, foregroundThread, false);
            }
        }
    }

    public static nint MonitorOf(nint hwnd) =>
        User32.MonitorFromWindow(hwnd, User32.MONITOR_DEFAULTTONEAREST);
}
