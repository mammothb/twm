using Twm.Adapters.Windows.Tests.Fixtures;
using Twm.Adapters.Windows.Tests.Helpers;
using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Adapters.Windows.Tests;

public sealed class WindowsWindowSystemTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void EnumerateWindows_IncludesTestWindow()
    {
        using var window = new TestWindow("twm-enum-test-" + Guid.NewGuid().ToString("N"));

        var ws = new WindowsWindowSystem();
        IReadOnlyList<NativeWindowInfo> windows = ws.EnumerateWindows();

        windows.ShouldContain(w => w.Id == new WindowId(window.Handle));
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void GetTitle_AfterSetWindowText_ReturnsNewText()
    {
        using var window = new TestWindow("twm-original");
        const string updated = "twm-updated";

        Win32.SetWindowTextW(window.Handle, updated);

        var ws = new WindowsWindowSystem();
        ws.GetTitle(new WindowId(window.Handle)).ShouldBe(updated);
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void GetTitle_UnicodeTitle_PreservesChars()
    {
        const string unicode = "twm-日本語-🎉-café";
        using var window = new TestWindow(unicode);

        var ws = new WindowsWindowSystem();
        ws.GetTitle(new WindowId(window.Handle)).ShouldBe(unicode);
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void SetWindowRect_NewBounds_ReflectedByDescribe()
    {
        using var window = new TestWindow("twm-bounds");

        var ws = new WindowsWindowSystem();
        ws.SetWindowRect(new WindowId(window.Handle), new Rect(100, 100, 800, 600));

        NativeWindowInfo info = ws.Describe(new WindowId(window.Handle));
        info.Bounds.X.ShouldBe(100);
        info.Bounds.Y.ShouldBe(100);
        info.Bounds.Width.ShouldBe(800);
        info.Bounds.Height.ShouldBe(600);
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Describe_NewMessageOnlyWindow_ReportsExpectedMetadata()
    {
        using var window = new TestWindow("twm-describe");

        var ws = new WindowsWindowSystem();
        NativeWindowInfo info = ws.Describe(new WindowId(window.Handle));

        info.Title.ShouldBe("twm-describe");
        info.ClassName.ShouldBe("Static");
        info.IsToolWindow.ShouldBeTrue();
        info.IsVisible.ShouldBeFalse();
        info.IsChild.ShouldBeFalse();
        info.IsMinimized.ShouldBeFalse();
        info.IsLayered.ShouldBeFalse();
        info.IsCloaked.ShouldBeFalse();
        info.Owner.ShouldBeNull();
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void HideThenShow_WindowStillEnumerates()
    {
        // ImmersiveShell cloak is a best-effort op on Server Core / minimal DWM
        // sessions; this test only asserts the window is still a real top-level
        // window after both calls, regardless of whether cloak actually fired.
        using var window = new TestWindow("twm-cloak");

        var ws = new WindowsWindowSystem();
        ws.Hide(new WindowId(window.Handle));
        ws.Show(new WindowId(window.Handle));

        ws.EnumerateWindows().ShouldContain(w => w.Id == new WindowId(window.Handle));
    }
}
