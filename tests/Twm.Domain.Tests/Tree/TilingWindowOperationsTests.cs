using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Domain.Tests.Tree;

public class TilingWindowOperationsTests
{
    [Fact]
    public void MoveInDirection_WhenParentIsNotSplitContainer_ReturnsFalse()
    {
        (TilingWindow window, Monitor monitor) = SingleWindowOnMonitor();
        window.MoveInDirection(Direction.Right).ShouldBeFalse();
        monitor.AppendChild(window);
        window.MoveInDirection(Direction.Right).ShouldBeFalse();
    }

    [Fact]
    public void MoveInDirection_WhenOnlyChildInSplit_ReturnsFalse()
    {
        var split = new SplitContainer();
        var window = new TilingWindow(new WindowId(1));
        split.AppendChild(window);
        window.MoveInDirection(Direction.Right).ShouldBeFalse();
    }

    [Fact]
    public void MoveInDirection_WhenSiblingInSameSplit_ReordersAndReturnsTrue()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        split.AppendChild(w1);
        split.AppendChild(w2);
        bool moved = w1.MoveInDirection(Direction.Right);
        moved.ShouldBeTrue();
        split.Children.ShouldBe([w2, w1]);
        w1.Parent.ShouldBeSameAs(split);
    }

    [Fact]
    public void MoveInDirection_WhenAdjacentSplitIsNonEmpty_DivesIntoNearEdge()
    {
        var outerSplit = new SplitContainer(Layout.SplitHorizontal);
        var w1 = new TilingWindow(new WindowId(1));
        var innerSplit = new SplitContainer(Layout.SplitVertical);
        var w2 = new TilingWindow(new WindowId(2));
        var w3 = new TilingWindow(new WindowId(3));
        outerSplit.AppendChild(w1);
        outerSplit.AppendChild(innerSplit);
        innerSplit.AppendChild(w2);
        innerSplit.AppendChild(w3);
        bool moved = w1.MoveInDirection(Direction.Right);
        moved.ShouldBeTrue();
        outerSplit.Children.ShouldBe([innerSplit]);
        innerSplit.Children[0].ShouldBeSameAs(w1);
        innerSplit.Children[1].ShouldBeSameAs(w2);
    }

    [Fact]
    public void MoveInDirection_WhenNestedDeeperThanMatchingSplit_PopsUpToAncestor()
    {
        // outer (Horizontal, axis matches) -> [middle, other]
        // middle (Vertical, axis does NOT match) -> [w1, w2]
        // Walking up from w1: parent (middle) doesn't match horizontal
        // axis; grandparent (outer) matches and pivot is middle (not w1),
        // so w1 pops out beside middle in outer. After Cleanup, middle
        // (now containing only w2) is flattened into outer.
        var outer = new SplitContainer(Layout.SplitHorizontal);
        var middle = new SplitContainer(Layout.SplitVertical);
        var other = new TilingWindow(new WindowId(99));
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        outer.AppendChild(middle);
        outer.AppendChild(other);
        middle.AppendChild(w1);
        middle.AppendChild(w2);

        bool moved = w1.MoveInDirection(Direction.Right);

        moved.ShouldBeTrue();
        outer.Children.ShouldBe([w2, w1, other]);
    }

    [Fact]
    public void MoveInDirection_WhenNoAncestorSplitMatchesAxis_ReturnsFalse()
    {
        var split = new SplitContainer(Layout.SplitVertical);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        split.AppendChild(w1);
        split.AppendChild(w2);
        w1.MoveInDirection(Direction.Right).ShouldBeFalse();
    }

    [Fact]
    public void MoveToAdjacentMonitor_WhenNoAdjacentMonitorInDirection_ReturnsFalse()
    {
        // Subject sits on the leftmost monitor; moving left finds no neighbor.
        var root = new RootContainer();
        var left = new Monitor(new Rect(0, 0, 100, 100));
        var right = new Monitor(new Rect(100, 0, 100, 100));
        root.AppendChild(left);
        root.AppendChild(right);

        var ws = new Workspace("left-ws");
        left.AppendChild(ws);
        var subject = new TilingWindow(new WindowId(1));
        ws.AppendChild(subject);

        right.AppendChild(new Workspace("right-ws"));

        bool moved = subject.MoveToAdjacentMonitor(Direction.Left);
        moved.ShouldBeFalse();
        left.Children.ShouldNotContain(right);
    }

    [Fact]
    public void MoveToAdjacentMonitor_WhenAdjacentMonitorExists_MovesToItsActiveWorkspace()
    {
        (TilingWindow window, _, Monitor right) = ThreeMonitorSetup();
        bool moved = window.MoveToAdjacentMonitor(Direction.Right);
        moved.ShouldBeTrue();
        Workspace? targetWorkspace = right.ActiveWorkspace;
        targetWorkspace.ShouldNotBeNull();
        targetWorkspace.Children.ShouldContain(window);
    }

    [Fact]
    public void MoveToAdjacentMonitor_WhenTargetWorkspaceIsEmpty_FallsBackToWorkspace()
    {
        (TilingWindow window, _, Monitor right) = TwoMonitorSetup();
        bool moved = window.MoveToAdjacentMonitor(Direction.Right);
        moved.ShouldBeTrue();
        right.ActiveWorkspace!.Children.ShouldContain(window);
    }

    [Fact]
    public void MoveToWorkspace_WhenTargetIsAlreadyCurrentWorkspace_ReturnsFalse()
    {
        (_, TilingWindow window, Workspace ws, Workspace otherWs) = TwoWorkspacesOnMonitor();
        bool moved = window.MoveToWorkspace(ws);
        moved.ShouldBeFalse();
        ws.Children.ShouldContain(window);
        otherWs.Children.ShouldNotContain(window);
    }

    [Fact]
    public void MoveToWorkspace_WhenTargetIsDifferent_AppendsToTargetWorkspace()
    {
        (_, TilingWindow window, Workspace ws, Workspace otherWs) = TwoWorkspacesOnMonitor();
        bool moved = window.MoveToWorkspace(otherWs);
        moved.ShouldBeTrue();
        ws.Children.ShouldNotContain(window);
        otherWs.Children.ShouldContain(window);
    }

    [Fact]
    public void Remove_WhenWindowIsDetached_IsNoOp()
    {
        var window = new TilingWindow(new WindowId(1));
        window.Remove();
    }

    [Fact]
    public void Remove_WhenWindowIsAttached_DetachesFromParent()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        split.AppendChild(w1);
        split.AppendChild(w2);
        w1.Remove();
        split.Children.ShouldBe([w2]);
        w1.Parent.ShouldBeNull();
    }

    [Fact]
    public void ResizeInDirection_WhenNoParent_ReturnsFalse()
    {
        var window = new TilingWindow(new WindowId(1));
        window.ResizeInDirection(Direction.Right, 0.1).ShouldBeFalse();
    }

    [Fact]
    public void ResizeInDirection_WhenNeighborExists_ResizesAndReturnsTrue()
    {
        var split = new SplitContainer(Layout.SplitHorizontal);
        var w1 = new TilingWindow(new WindowId(1)) { SizeFraction = 0.5 };
        var w2 = new TilingWindow(new WindowId(2)) { SizeFraction = 0.5 };
        split.AppendChild(w1);
        split.AppendChild(w2);
        bool resized = w1.ResizeInDirection(Direction.Right, 0.1);
        resized.ShouldBeTrue();
        w1.SizeFraction.ShouldBe(0.6);
        w2.SizeFraction.ShouldBe(0.4);
    }

    [Fact]
    public void ResizeInDirection_WhenOnlyChild_ReturnsFalse()
    {
        var split = new SplitContainer(Layout.SplitHorizontal);
        var w1 = new TilingWindow(new WindowId(1)) { SizeFraction = 1.0 };
        split.AppendChild(w1);
        w1.ResizeInDirection(Direction.Right, 0.1).ShouldBeFalse();
    }

    [Fact]
    public void ResizeWithNeighbor_WhenNoParent_ReturnsFalse()
    {
        var window = new TilingWindow(new WindowId(1));
        window.ResizeWithNeighbor(0.1).ShouldBeFalse();
    }

    [Fact]
    public void ResizeWithNeighbor_WhenNoSibling_ReturnsFalse()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        split.AppendChild(w1);
        w1.ResizeWithNeighbor(0.1).ShouldBeFalse();
    }

    [Fact]
    public void ResizeWithNeighbor_WhenSiblingExists_ResizesAndReturnsTrue()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1)) { SizeFraction = 0.5 };
        var w2 = new TilingWindow(new WindowId(2)) { SizeFraction = 0.5 };
        split.AppendChild(w1);
        split.AppendChild(w2);
        bool resized = w1.ResizeWithNeighbor(0.2);
        resized.ShouldBeTrue();
        w1.SizeFraction.ShouldBe(0.7);
        w2.SizeFraction.ShouldBe(0.3);
    }

    [Fact]
    public void SplitInDirection_WhenWindowHasNoParent_IsNoOp()
    {
        var window = new TilingWindow(new WindowId(1));
        window.SplitInDirection(TilingDirection.Horizontal);
    }

    [Fact]
    public void SplitInDirection_WhenOnlyChild_ReorientsParentLayout()
    {
        var split = new SplitContainer(Layout.SplitHorizontal);
        var w1 = new TilingWindow(new WindowId(1));
        split.AppendChild(w1);
        w1.SplitInDirection(TilingDirection.Vertical);
        split.Layout.ShouldBe(Layout.SplitVertical);
        split.Children.ShouldBe([w1]);
    }

    [Fact]
    public void SplitInDirection_WhenMultipleChildren_WrapsWindowInNewSplit()
    {
        var split = new SplitContainer(Layout.SplitHorizontal);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        split.AppendChild(w1);
        split.AppendChild(w2);
        w1.SplitInDirection(TilingDirection.Vertical);
        split.Children.Count.ShouldBe(2);
        split.Children[1].ShouldBeSameAs(w2);
        SplitContainer wrapper = split.Children[0].ShouldBeOfType<SplitContainer>();
        wrapper.Layout.ShouldBe(Layout.SplitVertical);
        wrapper.Children.ShouldBe([w1]);
    }

    private static (TilingWindow Window, Monitor Monitor) SingleWindowOnMonitor()
    {
        var monitor = new Monitor(new Rect(0, 0, 100, 100));
        return (new TilingWindow(new WindowId(1)), monitor);
    }

    private static (TilingWindow Window, Monitor Left, Monitor Right) ThreeMonitorSetup()
    {
        var root = new RootContainer();
        var left = new Monitor(new Rect(0, 0, 100, 100));
        var middle = new Monitor(new Rect(100, 0, 100, 100));
        var right = new Monitor(new Rect(200, 0, 100, 100));
        root.AppendChild(left);
        root.AppendChild(middle);
        root.AppendChild(right);
        var ws = new Workspace("mid");
        middle.AppendChild(ws);
        var subject = new TilingWindow(new WindowId(1));
        ws.AppendChild(subject);
        left.AppendChild(new Workspace("left-ws"));
        right.AppendChild(new Workspace("right-ws"));
        return (subject, left, right);
    }

    private static (TilingWindow Window, Monitor Left, Monitor Right) TwoMonitorSetup()
    {
        var root = new RootContainer();
        var left = new Monitor(new Rect(0, 0, 100, 100));
        var right = new Monitor(new Rect(100, 0, 100, 100));
        root.AppendChild(left);
        root.AppendChild(right);
        var ws = new Workspace("left-ws");
        left.AppendChild(ws);
        var subject = new TilingWindow(new WindowId(1));
        ws.AppendChild(subject);
        right.AppendChild(new Workspace("right-ws"));
        return (subject, left, right);
    }

    private static (
        Monitor Monitor,
        TilingWindow Window,
        Workspace Ws,
        Workspace OtherWs
    ) TwoWorkspacesOnMonitor()
    {
        var monitor = new Monitor(new Rect(0, 0, 100, 100));
        var ws = new Workspace("1");
        var otherWs = new Workspace("2");
        monitor.AppendChild(ws);
        monitor.AppendChild(otherWs);
        var window = new TilingWindow(new WindowId(1));
        ws.AppendChild(window);
        return (monitor, window, ws, otherWs);
    }
}
