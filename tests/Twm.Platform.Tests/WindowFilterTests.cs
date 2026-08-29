using Twm.Core.Geometry;
using Twm.Core.Tree;
using Twm.Platform.Config;

namespace Twm.Platform.Tests;

public class WindowFilterTests
{
    /// <summary>
    /// The default filter (no config rules), behaves like the built-in field
    /// predicate.
    /// </summary>
    private static readonly WindowFilter s_filter = new();

    private static NativeWindowInfo Manageable() =>
        new(
            Id: new WindowId(1),
            Title: "Editor",
            ClassName: "Notepad",
            Bounds: new Rect(0, 0, 800, 600),
            IsVisible: true,
            IsCloaked: false,
            IsToolWindow: false,
            IsMinimized: false
        );

    [Fact]
    public void NormalTopLevelWindow_IsManageable()
    {
        s_filter.IsManageable(Manageable()).ShouldBeTrue();
    }

    [Fact]
    public void InvisibleWindow_IsIgnored()
    {
        s_filter.IsManageable(Manageable() with { IsVisible = false }).ShouldBeFalse();
    }

    [Fact]
    public void CloakedWindow_IsIgnored()
    {
        s_filter.IsManageable(Manageable() with { IsCloaked = true }).ShouldBeFalse();
    }

    [Fact]
    public void MinimizedWindow_IsIgnored()
    {
        s_filter.IsManageable(Manageable() with { IsMinimized = true }).ShouldBeFalse();
    }

    [Fact]
    public void ToolWindow_IsIgnored()
    {
        s_filter.IsManageable(Manageable() with { IsToolWindow = true }).ShouldBeFalse();
    }

    [Fact]
    public void ChildWindow_IsIgnored()
    {
        s_filter
            .IsManageable(Manageable() with { Title = "Chrome Legacy Window", IsChild = true })
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void BlankTitle_IsIgnored(string title)
    {
        s_filter.IsManageable(Manageable() with { Title = title }).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Shell_TrayWnd")]
    [InlineData("Shell_SecondaryTrayWnd")]
    [InlineData("Progman")]
    [InlineData("WorkerW")]
    [InlineData("Window.UI.Core.CoreWindow")]
    [InlineData("TaskManagerWindow")]
    public void IgnoredClass_IsIgnored(string className)
    {
        s_filter.IsManageable(Manageable() with { ClassName = className }).ShouldBeFalse();
    }

    [Fact]
    public void ElevatedWindow_IsIgnored()
    {
        s_filter.IsManageable(Manageable() with { IsElevated = true }).ShouldBeFalse();
    }

    [Fact]
    public void NoActivateWindow_IsIgnored()
    {
        s_filter.IsManageable(Manageable() with { IsNoActivate = true }).ShouldBeFalse();
    }

    [Fact]
    public void OwnedMenuPopup_IsIgnored()
    {
        s_filter.IsManageable(Manageable() with { IsMenuPopup = true }).ShouldBeFalse();
    }

    [Fact]
    public void NoCaptionWindow_IsIgnored()
    {
        s_filter.IsManageable(Manageable() with { HasCaption = false }).ShouldBeFalse();
    }

    [Fact]
    public void NoWindowEdgeWindow_IsIgnored()
    {
        s_filter.IsManageable(Manageable() with { HasWindowEdge = false }).ShouldBeFalse();
    }

    [Fact]
    public void BorderlessWindow_WithManageRule_IsRescued()
    {
        var filter = new WindowFilter([new WindowRule("Notepad", null, WindowRuleAction.Manage)]);

        filter
            .IsManageable(Manageable() with { HasCaption = false, HasWindowEdge = false })
            .ShouldBeTrue();
    }

    [Fact]
    public void UwpAppHostClass_IsManageable()
    {
        s_filter
            .IsManageable(Manageable() with { ClassName = "ApplicationFrameWindow" })
            .ShouldBeTrue();
    }

    [Fact]
    public void ManageableWindow_WithIgnoreRule_IsIgnored()
    {
        var filter = new WindowFilter([new WindowRule("Notepad", null, WindowRuleAction.Ignore)]);

        filter.IsManageable(Manageable()).ShouldBeFalse();
    }

    [Fact]
    public void ConfigManageRule_RescuesWindowDroppedByDefaults()
    {
        var filter = new WindowFilter([new WindowRule(null, "Editor", WindowRuleAction.Manage)]);

        filter.IsManageable(Manageable() with { IsMenuPopup = true }).ShouldBeTrue();
    }

    [Fact]
    public void ConfigRule_DoesNotMatch_FallsBackToDefaults()
    {
        var filter = new WindowFilter([
            new WindowRule("SomeOtherClass", null, WindowRuleAction.Ignore),
        ]);

        filter.IsManageable(Manageable()).ShouldBeTrue();
    }
}
