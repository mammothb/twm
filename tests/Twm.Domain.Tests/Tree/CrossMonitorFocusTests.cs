using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Domain.Tests.Tree;

public class CrossMonitorFocusTests
{
    [Theory]
    [InlineData(Layout.Stacked)]
    [InlineData(Layout.Tabbed)]
    public void ReturnsToStackWorkspace_FocusesPreviouslyFocusedWindow(Layout layout)
    {
        (TilingWindow subject, _, TilingWindow w2, _) = TwoMonitorsWithStackOnLeft(layout);

        w2.Focus();

        subject.FindFocusTarget(Direction.Left).ShouldBeSameAs(w2);
    }

    [Fact]
    public void AlongAxisSplit_EntersEdgeTile_NotLastFocused()
    {
        (TilingWindow subject, TilingWindow w1, TilingWindow w2) = TwoMonitorsWithSplitOnLeft(
            Layout.SplitHorizontal
        );

        w1.Focus();

        subject.FindFocusTarget(Direction.Left).ShouldBeSameAs(w2);
    }

    [Fact]
    public void PerpendicularSplit_FromLowerHalf_EntersBottom()
    {
        (TilingWindow subject, TilingWindow w1, TilingWindow w2) = TwoMonitorsWithSplitOnLeft(
            Layout.SplitVertical
        );

        w1.Focus();
        subject.Bounds = new Rect(1920, 540, 1920, 540);

        subject.FindFocusTarget(Direction.Left).ShouldBeSameAs(w2);
    }

    [Fact]
    public void PerpendicularSplit_FromUpperHalf_EntersTop()
    {
        (TilingWindow subject, TilingWindow w1, TilingWindow w2) = TwoMonitorsWithSplitOnLeft(
            Layout.SplitVertical
        );

        w2.Focus();
        subject.Bounds = new Rect(1920, 0, 1920, 540);

        subject.FindFocusTarget(Direction.Left).ShouldBeSameAs(w1);
    }

    [Fact]
    public void TabbedTileInsideHorizontalSplit_RestoresFocusedTab()
    {
        (TilingWindow subject, _, _, TilingWindow w3, _) = TwoMonitorsWithTabbledTileOnLeft();

        w3.Focus();

        subject.FindFocusTarget(Direction.Left).ShouldBeSameAs(w3);
    }

    private static (
        TilingWindow Subject,
        TilingWindow W1,
        TilingWindow W2,
        TilingWindow W3
    ) TwoMonitorsWithStackOnLeft(Layout layout)
    {
        var root = new RootContainer();

        var left = new Monitor(new Rect(0, 0, 1920, 1080));
        var stackWs = new Workspace("1", layout);
        left.AppendChild(stackWs);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        var w3 = new TilingWindow(new WindowId(3));
        stackWs.AppendChild(w1);
        stackWs.AppendChild(w2);
        stackWs.AppendChild(w3);

        var right = new Monitor(new Rect(1920, 0, 1920, 1080));
        var originWs = new Workspace("2");
        right.AppendChild(originWs);
        var subject = new TilingWindow(new WindowId(4));
        originWs.AppendChild(subject);

        root.AppendChild(left);
        root.AppendChild(right);

        new LayoutEngine().Arrange(root);

        return (subject, w1, w2, w3);
    }

    private static (
        TilingWindow Subject,
        TilingWindow W1,
        TilingWindow W2
    ) TwoMonitorsWithSplitOnLeft(Layout layout)
    {
        var root = new RootContainer();

        var left = new Monitor(new Rect(0, 0, 1920, 1080));
        var stackWs = new Workspace("1", layout);
        left.AppendChild(stackWs);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        stackWs.AppendChild(w1);
        stackWs.AppendChild(w2);

        var right = new Monitor(new Rect(1920, 0, 1920, 1080));
        var originWs = new Workspace("2");
        right.AppendChild(originWs);
        var subject = new TilingWindow(new WindowId(3));
        originWs.AppendChild(subject);

        root.AppendChild(left);
        root.AppendChild(right);

        new LayoutEngine().Arrange(root);

        return (subject, w1, w2);
    }

    private static (
        TilingWindow Subject,
        TilingWindow W1,
        TilingWindow W2,
        TilingWindow W3,
        TilingWindow W4
    ) TwoMonitorsWithTabbledTileOnLeft()
    {
        var root = new RootContainer();

        var left = new Monitor(new Rect(0, 0, 1920, 1080));
        var ws = new Workspace("1", Layout.SplitHorizontal);
        left.AppendChild(ws);
        var w1 = new TilingWindow(new WindowId(1));
        ws.AppendChild(w1);
        var tabbed = new SplitContainer(Layout.Tabbed);
        ws.AppendChild(tabbed);
        var w2 = new TilingWindow(new WindowId(2));
        var w3 = new TilingWindow(new WindowId(3));
        var w4 = new TilingWindow(new WindowId(4));
        tabbed.AppendChild(w2);
        tabbed.AppendChild(w3);
        tabbed.AppendChild(w4);

        var right = new Monitor(new Rect(1920, 0, 1920, 1080));
        var originWs = new Workspace("2");
        right.AppendChild(originWs);
        var subject = new TilingWindow(new WindowId(4));
        originWs.AppendChild(subject);

        root.AppendChild(left);
        root.AppendChild(right);

        new LayoutEngine().Arrange(root);

        return (subject, w1, w2, w3, w4);
    }
}
