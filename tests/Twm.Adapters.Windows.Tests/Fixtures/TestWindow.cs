using Twm.Adapters.Windows.Tests.Helpers;

namespace Twm.Adapters.Windows.Tests.Fixtures;

/// <summary>
/// A real top-level Win32 window for end-to-end tests of the Win32 P/Invoke
/// surface. <c>WS_EX_TOOLWINDOW</c> hides it from the taskbar and Alt+Tab, and
/// no <c>ShowWindow</c> is called so it never paints — safe for CI.
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
            hWndParent: 0,
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
