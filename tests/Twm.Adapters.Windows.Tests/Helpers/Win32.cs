using System.Runtime.InteropServices;

namespace Twm.Adapters.Windows.Tests.Helpers;

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct Rect32(int Left, int Top, int Right, int Bottom);

/// <summary>
/// Win32 P/Invoke for the test HWND fixture. Source-generated via
/// <see cref="LibraryImportAttribute" />; compiles on every platform but only
/// resolves to user32.dll at runtime on Windows.
///
/// <para>
/// <c>DwmGetWindowFrameBounds</c> and <c>Rect32</c> are duplicated from
/// <see cref="Twm.Adapters.Windows.NativeMethods" /> because the production
/// version is <c>private</c>. Touching production visibility just to read
/// the visible rect in tests isn't worth the SA1202 layout churn.
/// </para>
/// </summary>
internal static partial class Win32
{
    // WS_EX_TOOLWINDOW: keep the test window out of the taskbar and Alt+Tab so
    // CI runs never flash a visible window while a test is creating fixtures.
    internal const uint WsExToolwindow = 0x00000080;

    // DWMWA_EXTENDED_FRAME_BOUNDS
    internal const uint DwmwaExtendedFrameBounds = 9;

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint CreateWindowExW(
        uint dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x,
        int y,
        int nWidth,
        int nHeight,
        nint hWndParent,
        nint hMenu,
        nint hInstance,
        nint lpParam
    );

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyWindow(nint hWnd);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowTextW(nint hWnd, string lpString);

    [LibraryImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")]
    internal static partial int DwmGetWindowFrameBounds(
        nint hWnd,
        uint dwAttribute,
        out Rect32 pvAttribute,
        int cbAttribute
    );
}
