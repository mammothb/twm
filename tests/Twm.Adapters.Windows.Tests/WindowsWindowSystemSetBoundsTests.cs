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
    public void Describe_AfterSetWindowRect_OuterAndVisibleDifferByUniformInset()
    {
        // Describe reads the OUTER rect (what GetWindowRect returns).
        // SetBounds widens that rect by the DWM frame inset on every side so
        // the visible rect equals the target. This test verifies the four
        // insets are equal (catches typos like missing a side, e.g.
        // `cx = bounds.Width + left` instead of `+ left + right`).
        using var window = new TestWindow("twm-bounds-after");

        var ws = new WindowsWindowSystem();
        var target = new Rect(100, 100, 800, 600);
        ws.SetWindowRect(new WindowId(window.Handle), target);

        NativeWindowInfo info = ws.Describe(new WindowId(window.Handle));
        Rect visible = QueryVisibleRect(window.Handle);

        int leftInset = visible.X - info.Bounds.X;
        int rightInset = info.Bounds.Right - visible.Right;
        int topInset = visible.Y - info.Bounds.Y;
        int bottomInset = info.Bounds.Bottom - visible.Bottom;

        leftInset.ShouldBe(rightInset);
        topInset.ShouldBe(bottomInset);
        leftInset.ShouldBe(topInset);
        leftInset.ShouldBeGreaterThan(0); // DWM inset is always > 0
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
