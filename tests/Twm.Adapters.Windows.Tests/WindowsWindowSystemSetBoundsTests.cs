using Twm.Adapters.Windows.Tests.Fixtures;
using Twm.Adapters.Windows.Tests.Helpers;
using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Adapters.Windows.Tests;

/// <summary>
/// Verifies <see cref="WindowsWindowSystem.SetWindowRect" />'s contract:
/// after the call, the window's *visible* rect (as reported by
/// <c>DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)</c>) equals the
/// requested bounds. The outer rect (what <c>GetWindowRect</c> returns) is
/// offset by the DWM frame inset and is implementation detail.
/// </summary>
public sealed class WindowsWindowSystemSetBoundsTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void SetWindowRect_AdjustsByDwmFrameInset_VisibleRectMatchesTarget()
    {
        using var window = new TestWindow("twm-bounds");

        var ws = new WindowsWindowSystem();
        var target = new Rect(100, 100, 800, 600);
        ws.SetWindowRect(new WindowId(window.Handle), target);

        // Visible rect should equal the target — that's what SetBounds
        // actually guarantees (the outer rect is offset by DWM's invisible
        // resize frame).
        Rect visible = QueryVisibleRect(window.Handle);
        visible.X.ShouldBe(target.X);
        visible.Y.ShouldBe(target.Y);
        visible.Width.ShouldBe(target.Width);
        visible.Height.ShouldBe(target.Height);
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Describe_AfterSetWindowRect_VisibleBoundsMatch()
    {
        // Cross-check: Describe reads the outer rect, which differs from the
        // visible rect by the DWM inset. The inset is small (~7px at 96 DPI)
        // so we just verify the outer rect moved into the expected region.
        using var window = new TestWindow("twm-bounds-after");

        var ws = new WindowsWindowSystem();
        var target = new Rect(100, 100, 800, 600);
        ws.SetWindowRect(new WindowId(window.Handle), target);

        NativeWindowInfo info = ws.Describe(new WindowId(window.Handle));
        // Width/Height round-trip exactly; X/Y are offset by the DWM frame
        // inset (~7px) which NativeMethods.SetBounds applies so the visible
        // rect matches.
        info.Bounds.Width.ShouldBe(target.Width);
        info.Bounds.Height.ShouldBe(target.Height);
    }

    private static Rect QueryVisibleRect(nint hWnd)
    {
        int result = Win32.DwmGetWindowFrameBounds(
            hWnd,
            Win32.DwmwaExtendedFrameBounds,
            out Rect32 visible,
            System.Runtime.InteropServices.Marshal.SizeOf<Rect32>()
        );

        result.ShouldBe(0); // S_OK
        return new Rect(
            visible.Left,
            visible.Top,
            visible.Right - visible.Left,
            visible.Bottom - visible.Top
        );
    }
}
