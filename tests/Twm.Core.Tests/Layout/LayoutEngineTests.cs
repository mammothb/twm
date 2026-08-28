using Twm.Core.Geometry;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Tests.Layout;

public class LayoutEngineTests
{
    [Fact]
    public void Arrange_SingleWindowFillsWorkspace()
    {
        var displayBounds = new Rect(0, 0, 800, 600);
        var monitor = new Monitor(displayBounds);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var window = new TilingWindow(new WindowId(1));
        workspace.AppendChild(window);

        new LayoutEngine().Arrange(monitor);

        window.Bounds.ShouldBe(displayBounds);
    }

    [Fact]
    public void Arrange_HorizontalSplitDividesWidthEqually()
    {
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var left = new TilingWindow(new WindowId(1));
        var right = new TilingWindow(new WindowId(2));
        workspace.AppendChild(left);
        workspace.AppendChild(right);

        new LayoutEngine().Arrange(monitor);

        left.Bounds.ShouldBe(new Rect(0, 0, 400, 600));
        right.Bounds.ShouldBe(new Rect(400, 0, 400, 600));
    }

    [Fact]
    public void Arrange_VerticalSplitDividesHeightEqually()
    {
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        var workspace = new Workspace("1", LayoutMode.SplitVertical);
        monitor.AppendChild(workspace);
        var top = new TilingWindow(new WindowId(1));
        var bottom = new TilingWindow(new WindowId(2));
        workspace.AppendChild(top);
        workspace.AppendChild(bottom);

        new LayoutEngine().Arrange(monitor);

        top.Bounds.ShouldBe(new Rect(0, 0, 800, 300));
        bottom.Bounds.ShouldBe(new Rect(0, 300, 800, 300));
    }

    [Fact]
    public void Arrange_UnevenSizeFractionsDivideProportionally()
    {
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var small = new TilingWindow(new WindowId(1)) { SizeFraction = 1 };
        var large = new TilingWindow(new WindowId(2)) { SizeFraction = 3 };
        workspace.AppendChild(small);
        workspace.AppendChild(large);

        new LayoutEngine().Arrange(monitor);

        small.Bounds.ShouldBe(new Rect(0, 0, 200, 600));
        large.Bounds.ShouldBe(new Rect(200, 0, 600, 600));
    }

    [Fact]
    public void Arrange_NestedSplitLaysOutRecursively()
    {
        var monitor = new Monitor(new Rect(0, 0, 1200, 800));
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var left = new TilingWindow(new WindowId(1));
        workspace.AppendChild(left);
        var right = new SplitContainer(LayoutMode.SplitVertical);
        workspace.AppendChild(right);
        var rightTop = new TilingWindow(new WindowId(2));
        var rightBottom = new TilingWindow(new WindowId(3));
        right.AppendChild(rightTop);
        right.AppendChild(rightBottom);

        new LayoutEngine().Arrange(monitor);

        left.Bounds.ShouldBe(new Rect(0, 0, 600, 800));
        right.Bounds.ShouldBe(new Rect(600, 0, 600, 800));
        rightTop.Bounds.ShouldBe(new Rect(600, 0, 600, 400));
        rightBottom.Bounds.ShouldBe(new Rect(600, 400, 600, 400));
    }

    [Fact]
    public void Arrange_WithGaps_InsetWorkspaceAndSeparateWindows()
    {
        var monitor = new Monitor(new Rect(0, 0, 1000, 1000));
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var left = new TilingWindow(new WindowId(1));
        var right = new TilingWindow(new WindowId(2));
        workspace.AppendChild(left);
        workspace.AppendChild(right);

        new LayoutEngine(new GapConfig(Inner: 10, Outer: 20)).Arrange(monitor);

        // Workspace inset by 20 on all sides -> (20, 20, 960, 960); one 10px
        // inner gap between two windows -> each 475 wide
        left.Bounds.ShouldBe(new Rect(20, 20, 475, 960));
        right.Bounds.ShouldBe(new Rect(505, 20, 475, 960));
        (right.Bounds.X - left.Bounds.Right).ShouldBe(10);
        right.Bounds.Right.ShouldBe(monitor.Bounds.Right - 20);
    }

    [Fact]
    public void Arrange_RemainderPixelsGoToLastSlice()
    {
        var monitor = new Monitor(new Rect(0, 0, 100, 10));
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        var w3 = new TilingWindow(new WindowId(3));
        workspace.AppendChild(w1);
        workspace.AppendChild(w2);
        workspace.AppendChild(w3);

        new LayoutEngine().Arrange(monitor);

        w1.Bounds.Width.ShouldBe(33);
        w2.Bounds.Width.ShouldBe(33);
        w3.Bounds.Width.ShouldBe(34);
        w3.Bounds.Right.ShouldBe(100);
    }

    [Fact]
    public void Arrange_RootLaysOutEveryMonitorIndependently()
    {
        var root = new RootContainer();
        var primaryBounds = new Rect(0, 0, 800, 600);
        var secondaryBounds = new Rect(800, 0, 1024, 768);
        var primary = new Monitor(primaryBounds);
        var secondary = new Monitor(secondaryBounds);
        root.AppendChild(primary);
        root.AppendChild(secondary);
        var ws1 = new Workspace("1");
        var ws2 = new Workspace("2");
        primary.AppendChild(ws1);
        secondary.AppendChild(ws2);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        ws1.AppendChild(w1);
        ws2.AppendChild(w2);

        new LayoutEngine().Arrange(root);

        w1.Bounds.ShouldBe(primaryBounds);
        w2.Bounds.ShouldBe(secondaryBounds);
    }

    [Fact]
    public void Arrange_TreeRendersWithComputedBounds()
    {
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        workspace.AppendChild(new TilingWindow(new WindowId(1)));
        workspace.AppendChild(new TilingWindow(new WindowId(2)));

        new LayoutEngine().Arrange(monitor);

        string expected =
            "Monitor [0,0 800x600]\n"
            + "  Workspace \"1\" Horizontal [0,0 800x600]\n"
            + "    Window #1 [0,0 400x600]\n"
            + "    Window #2 [400,0 400x600]\n";

        TreeRenderer.Render(monitor).ShouldBe(expected);
    }

    [Fact]
    public void TabbedLayout_GivesEveryChildTheContentRectBelowOneTitleRow()
    {
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        var workspace = new Workspace("1", LayoutMode.Tabbed);
        monitor.AppendChild(workspace);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        workspace.AppendChild(w1);
        workspace.AppendChild(w2);

        new LayoutEngine().Arrange(monitor);

        // One reserved title row (default 24px); both children fill the content
        // below it (only the focused one is shown on screen)
        var content = new Rect(0, 24, 800, 576);
        w1.Bounds.ShouldBe(content);
        w2.Bounds.ShouldBe(content);
    }

    [Fact]
    public void StackedLayout_ReservesOneTitleRowPerChild()
    {
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        var workspace = new Workspace("1", LayoutMode.Stacked);
        monitor.AppendChild(workspace);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        var w3 = new TilingWindow(new WindowId(3));
        workspace.AppendChild(w1);
        workspace.AppendChild(w2);
        workspace.AppendChild(w3);

        new LayoutEngine().Arrange(monitor);

        // Three children -> strip = 3 * 24 = 72; each fills the content below
        var content = new Rect(0, 72, 800, 528);
        w1.Bounds.ShouldBe(content);
        w2.Bounds.ShouldBe(content);
        w3.Bounds.ShouldBe(content);
    }

    [Fact]
    public void TabbedLayout_TitleBarHeightIsConfigurable()
    {
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        var workspace = new Workspace("1", LayoutMode.Tabbed);
        monitor.AppendChild(workspace);
        var w1 = new TilingWindow(new WindowId(1));
        workspace.AppendChild(w1);

        new LayoutEngine(GapConfig.None, 40).Arrange(monitor);

        w1.Bounds.ShouldBe(new Rect(0, 40, 800, 560));
    }

    [Fact]
    public void TabbedContainer_NestedInSplit_ReservesStripWithinItsColumn()
    {
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var left = new TilingWindow(new WindowId(1));
        var rightTabbed = new SplitContainer(LayoutMode.Tabbed);
        workspace.AppendChild(left);
        workspace.AppendChild(rightTabbed);
        var w2 = new TilingWindow(new WindowId(2));
        var w3 = new TilingWindow(new WindowId(3));
        rightTabbed.AppendChild(w2);
        rightTabbed.AppendChild(w3);

        new LayoutEngine().Arrange(monitor);

        // Left half is a plain window; the right half is a tabbed column whose
        // tab strip takes its top 24px and whose tab contents fill below,
        // proving the strip nests within a sub-rect
        left.Bounds.ShouldBe(new Rect(0, 0, 400, 600));
        rightTabbed.Bounds.ShouldBe(new Rect(400, 0, 400, 600));
        var content = new Rect(400, 24, 400, 576);
        w2.Bounds.ShouldBe(content);
        w3.Bounds.ShouldBe(content);
    }
}
