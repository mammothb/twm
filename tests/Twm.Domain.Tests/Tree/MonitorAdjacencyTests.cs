using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Domain.Tests.Tree;

public class MonitorAdjacencyTests
{
    [Fact]
    public void FindAdjacentMonitor_WhenCalledRight_ReturnsMonitorToTheRight()
    {
        (Monitor left, Monitor right) = TwoSideBySide();

        left.FindAdjacentMonitor(Direction.Right).ShouldBeSameAs(right);
    }

    [Fact]
    public void FindAdjacentMonitor_WhenCalledLeft_ReturnsMonitorToLeft()
    {
        (Monitor left, Monitor right) = TwoSideBySide();

        right.FindAdjacentMonitor(Direction.Left).ShouldBeSameAs(left);
    }

    [Fact]
    public void FindAdjacentMonitor_WhenNoMonitorInDirection_ReturnsNull()
    {
        (Monitor left, Monitor right) = TwoSideBySide();

        right.FindAdjacentMonitor(Direction.Right).ShouldBeNull();
        left.FindAdjacentMonitor(Direction.Left).ShouldBeNull();
    }

    [Fact]
    public void FindAdjacentMonitor_WhenMonitorsAreSideBySide_HasNoVerticalNeighbor()
    {
        (Monitor left, _) = TwoSideBySide();

        left.FindAdjacentMonitor(Direction.Up).ShouldBeNull();
        left.FindAdjacentMonitor(Direction.Down).ShouldBeNull();
    }

    [Fact]
    public void FindAdjacentMonitor_WhenCalledUp_ReturnsMonitorAbove()
    {
        (Monitor bottom, Monitor top) = TwoStackedVertically();

        bottom.FindAdjacentMonitor(Direction.Up).ShouldBeSameAs(top);
    }

    [Fact]
    public void FindAdjacentMonitor_WhenCalledDown_ReturnsMonitorBelow()
    {
        (Monitor bottom, Monitor top) = TwoStackedVertically();

        top.FindAdjacentMonitor(Direction.Down).ShouldBeSameAs(bottom);
    }

    [Fact]
    public void FindAdjacentMonitor_WhenMultipleMonitorsInSameDirection_ReturnsNearest()
    {
        // Three monitors to the right of `subject`, at different distances.
        // Subject is at X=0; others at X=100, X=200, X=300.
        (Monitor subject, _, _, Monitor near) = FourHorizontally();

        subject.FindAdjacentMonitor(Direction.Right).ShouldBeSameAs(near);
    }

    [Fact]
    public void FindAdjacentMonitor_WhenParentIsNotRootContainer_ReturnsNull()
    {
        // Monitor parented to a Workspace instead of a RootContainer. The
        // method requires RootContainer to enumerate siblings.
        var monitor = new Monitor(new Rect(0, 0, 100, 100));
        var workspace = new Workspace("1");
        workspace.AppendChild(monitor);
        var otherMonitor = new Monitor(new Rect(100, 0, 100, 100));
        // Other monitor lives in a different RootContainer; no shared parent.
        var otherRoot = new RootContainer();
        otherRoot.AppendChild(otherMonitor);

        monitor.FindAdjacentMonitor(Direction.Right).ShouldBeNull();
    }

    [Fact]
    public void FindAdjacentMonitor_WhenNoPerpendicularOverlap_ReturnsNull()
    {
        // Two monitors in the same X-axis direction but with no vertical
        // overlap — they're stacked diagonally, not truly adjacent.
        var root = new RootContainer();
        var left = new Monitor(new Rect(0, 0, 100, 50)); // Y range: 0..50
        var right = new Monitor(new Rect(200, 100, 100, 50)); // Y range: 100..150
        root.AppendChild(left);
        root.AppendChild(right);

        // Right is to the right of left (X-wise) but with no Y overlap.
        left.FindAdjacentMonitor(Direction.Right).ShouldBeNull();
    }

    [Fact]
    public void FindAdjacentMonitor_WhenDirectionIsUndefined_ReturnsNull()
    {
        // The default switch arm in IsInDirection (`_ => false`) makes
        // IsInDirection return false for any undefined Direction value, so
        // FindAdjacentMonitor finds no neighbor.
        (Monitor left, _) = TwoSideBySide();

        left.FindAdjacentMonitor((Direction)999).ShouldBeNull();
    }

    // Two monitors side by side: left 1920x1080 at origin, right 1280x1024 to
    // its right.
    private static (Monitor Left, Monitor Right) TwoSideBySide()
    {
        var root = new RootContainer();
        var left = new Monitor(new Rect(0, 0, 1920, 1080));
        var right = new Monitor(new Rect(1920, 0, 1280, 1024));
        root.AppendChild(left);
        root.AppendChild(right);
        return (left, right);
    }

    // Two monitors vertically stacked: top 1000x100 at origin, bottom
    // 1000x100 below it.
    private static (Monitor Bottom, Monitor Top) TwoStackedVertically()
    {
        var root = new RootContainer();
        var top = new Monitor(new Rect(0, 0, 1000, 100));
        var bottom = new Monitor(new Rect(0, 100, 1000, 100));
        root.AppendChild(top);
        root.AppendChild(bottom);
        return (bottom, top);
    }

    // Four monitors horizontally: subject at X=0, then near (X=100), mid
    // (X=200), far1 (X=300). All same height for vertical overlap.
    private static (Monitor Subject, Monitor Far1, Monitor Mid, Monitor Near) FourHorizontally()
    {
        var root = new RootContainer();
        var subject = new Monitor(new Rect(0, 0, 100, 100));
        var near = new Monitor(new Rect(100, 0, 100, 100));
        var mid = new Monitor(new Rect(200, 0, 100, 100));
        var far1 = new Monitor(new Rect(300, 0, 100, 100));
        root.AppendChild(subject);
        root.AppendChild(near);
        root.AppendChild(mid);
        root.AppendChild(far1);
        return (subject, far1, mid, near);
    }
}
