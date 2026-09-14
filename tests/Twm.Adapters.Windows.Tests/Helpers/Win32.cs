using System.Runtime.InteropServices;

namespace Twm.Adapters.Windows.Tests.Helpers;

/// <summary>
/// Win32 P/Invoke for the test HWND fixture. Source-generated via
/// <see cref="LibraryImportAttribute" />; compiles on every platform but only
/// resolves to user32.dll at runtime on Windows.
/// </summary>
internal static partial class Win32
{
    // WS_EX_TOOLWINDOW: keep the test window out of the taskbar and Alt+Tab so
    // CI runs never flash a visible window while a test is creating fixtures.
    internal const uint WsExToolwindow = 0x00000080;

    // HWND_MESSAGE = (HWND)-3: message-only windows have no UI and never paint,
    // but participate in EnumWindows/SetWinEventHook/SetWindowPos.
    internal static readonly nint HwndMessage = -3;

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
}
