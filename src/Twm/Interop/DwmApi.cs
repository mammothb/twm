using System.Runtime.InteropServices;

namespace Twm.Interop;

internal static partial class DwmApi
{
    private const uint DWMWA_EXTENDED_FRAME_BOUNDS = 9;
    private const uint DWMWA_CLOAKED = 14;

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttributeRect(
        nint hwnd,
        uint attribute,
        out User32.RECT value,
        int size
    );

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttributeDword(
        nint hwnd,
        uint attribute,
        out int value,
        int size
    );

    /// <summary>
    /// Visible bounds of a window, excluding invisible resize borders.
    /// Falls back to GetWindowRect when DWM reports nothing (rare).
    /// </summary>
    public static Layout.Rect GetExtendedFrameBounds(nint hwnd)
    {
        if (
            Succeeded(
                DwmGetWindowAttributeRect(
                    hwnd,
                    DWMWA_EXTENDED_FRAME_BOUNDS,
                    out var rect,
                    sizeof(int) * 4
                )
            )
        )
            return Layout.Rect.FromLtrb(rect.Left, rect.Top, rect.Right, rect.Bottom);

        User32.GetWindowRect(hwnd, out var fallback);
        return Layout.Rect.FromLtrb(fallback.Left, fallback.Top, fallback.Right, fallback.Bottom);
    }

    /// <summary>True when the window exists but is hidden behind a cloak
    /// (suspended UWP apps, virtual-desktop-hidden windows).</summary>
    public static bool IsCloaked(nint hwnd)
    {
        return Succeeded(
                DwmGetWindowAttributeDword(hwnd, DWMWA_CLOAKED, out int cloaked, sizeof(int))
            )
            && cloaked != 0;
    }

    private static bool Succeeded(int hr) => hr == 0; // S_OK
}
