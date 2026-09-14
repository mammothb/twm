using Twm.Adapters.Windows.Tests.Helpers;

namespace Twm.Adapters.Windows.Tests.Fixtures;

/// <summary>
/// A real but invisible Win32 window for end-to-end tests of the Win32 P/Invoke
/// surface. Message-only parent (<c>HWND_MESSAGE</c>) + <c>WS_EX_TOOLWINDOW</c>
/// means the window has no UI, doesn't paint, and never appears in the taskbar
/// or Alt+Tab — safe for CI.
/// </summary>
internal sealed class TestWindow : IDisposable
{
    public TestWindow(string title)
    {
        Handle = Win32.CreateWindowExW(
            dwExStyle: Win32.WsExToolwindow,
            lpClassName: "Static",
            lpWindowName: title,
            dwStyle: 0,
            x: 0,
            y: 0,
            nWidth: 0,
            nHeight: 0,
            hWndParent: Win32.HwndMessage,
            hMenu: 0,
            hInstance: 0,
            lpParam: 0
        );

        if (Handle == 0)
        {
            throw new InvalidOperationException("CreateWindowExW returned NULL");
        }
    }

    public nint Handle { get; }

    public void Dispose()
    {
        if (Handle != 0)
        {
            Win32.DestroyWindow(Handle);
        }
    }
}
