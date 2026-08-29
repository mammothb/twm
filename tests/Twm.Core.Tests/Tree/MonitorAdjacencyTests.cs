using Twm.Core.Geometry;
using Twm.Core.Tree;

namespace Twm.Core.Tests.Tree;

public class MonitorAdjacencyTests
{
    [Fact]
    public void Right_FindsMonitorToTheRight()
    {
        (Monitor left, Monitor right) = TwoSideBySide();

        left.AdjacentMonitor(Direction.Right).ShouldBe(right);
    }

    [Fact]
    public void Left_FindsMonitorToLeft()
    {
        (Monitor left, Monitor right) = TwoSideBySide();

        right.AdjacentMonitor(Direction.Left).ShouldBe(left);
    }

    [Fact]
    public void NoNeighborFurtherInDirection_ReturnsNull()
    {
        (Monitor left, Monitor right) = TwoSideBySide();

        right.AdjacentMonitor(Direction.Right).ShouldBeNull();
        left.AdjacentMonitor(Direction.Left).ShouldBeNull();
    }

    [Fact]
    public void SideBySideMonitors_HaveNoVerticalNeighbor()
    {
        (Monitor left, _) = TwoSideBySide();

        left.AdjacentMonitor(Direction.Up).ShouldBeNull();
        left.AdjacentMonitor(Direction.Down).ShouldBeNull();
    }

    // Two monitors side by side: left 1920x1080 at origin, right 1280x1024 to
    // its right
    private static (Monitor Left, Monitor Right) TwoSideBySide()
    {
        var root = new RootContainer();
        var left = new Monitor(new Rect(0, 0, 1920, 1080));
        var right = new Monitor(new Rect(1920, 0, 1280, 1024));
        root.AppendChild(left);
        root.AppendChild(right);
        return (left, right);
    }
}
