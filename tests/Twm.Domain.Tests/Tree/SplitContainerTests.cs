using Twm.Domain.Tree;

namespace Twm.Domain.Tests.Tree;

public class SplitContainerTests
{
    [Fact]
    public void ToggleSplitDirection_WhenCurrentIsSplitHorizontal_BecomesSplitVertical()
    {
        var split = new SplitContainer(Layout.SplitHorizontal);

        split.ToggleSplitDirection();

        split.Layout.ShouldBe(Layout.SplitVertical);
    }

    [Fact]
    public void ToggleSplitDirection_WhenCurrentIsSplitVertical_BecomesSplitHorizontal()
    {
        var split = new SplitContainer(Layout.SplitVertical);

        split.ToggleSplitDirection();

        split.Layout.ShouldBe(Layout.SplitHorizontal);
    }

    [Fact]
    public void ToggleSplitDirection_WhenCurrentIsTabbed_BecomesSplitHorizontal()
    {
        var split = new SplitContainer(Layout.Tabbed);

        split.ToggleSplitDirection();

        split.Layout.ShouldBe(Layout.SplitHorizontal);
    }

    [Fact]
    public void ToggleSplitDirection_WhenCurrentIsStacked_BecomesSplitHorizontal()
    {
        var split = new SplitContainer(Layout.Stacked);

        split.ToggleSplitDirection();

        split.Layout.ShouldBe(Layout.SplitHorizontal);
    }

    [Fact]
    public void TryResizeChild_WhenNeighborIsNull_ReturnsFalse()
    {
        var split = new SplitContainer();
        var child = new TilingWindow(new WindowId(1)) { SizeFraction = 0.5 };
        split.AppendChild(child);

        bool result = split.TryResizeChild(child, 0.1, neighbor: null);

        result.ShouldBeFalse();
        child.SizeFraction.ShouldBe(0.5);
    }

    [Fact]
    public void TryResizeChild_WhenBothFractionsStayAboveMinimum_AdjustsAndReturnsTrue()
    {
        var split = new SplitContainer();
        var child = new TilingWindow(new WindowId(1)) { SizeFraction = 0.5 };
        var neighbor = new TilingWindow(new WindowId(2)) { SizeFraction = 0.5 };
        split.AppendChild(child);
        split.AppendChild(neighbor);

        bool result = split.TryResizeChild(child, 0.2, neighbor);

        result.ShouldBeTrue();
        child.SizeFraction.ShouldBe(0.7);
        neighbor.SizeFraction.ShouldBe(0.3);
    }

    [Fact]
    public void TryResizeChild_WhenChildWouldDropBelowMinimum_ReturnsFalse()
    {
        var split = new SplitContainer();
        var child = new TilingWindow(new WindowId(1)) { SizeFraction = 0.05 };
        var neighbor = new TilingWindow(new WindowId(2)) { SizeFraction = 0.95 };
        split.AppendChild(child);
        split.AppendChild(neighbor);

        // Child would go to 0.05 + 0.1 = 0.15 (>= 0.1, OK), neighbor to 0.85.
        // Use a larger delta so child goes negative: actually delta = -0.2
        // would take child to -0.15 < 0.1.
        bool result = split.TryResizeChild(child, -0.2, neighbor);

        result.ShouldBeFalse();
        child.SizeFraction.ShouldBe(0.05);
        neighbor.SizeFraction.ShouldBe(0.95);
    }

    [Fact]
    public void TryResizeChild_WhenNeighborWouldDropBelowMinimum_ReturnsFalse()
    {
        var split = new SplitContainer();
        var child = new TilingWindow(new WindowId(1)) { SizeFraction = 0.5 };
        var neighbor = new TilingWindow(new WindowId(2)) { SizeFraction = 0.12 };
        split.AppendChild(child);
        split.AppendChild(neighbor);

        // Child grows by 0.1 to 0.6, neighbor shrinks by 0.1 to 0.02 < 0.1.
        bool result = split.TryResizeChild(child, 0.1, neighbor);

        result.ShouldBeFalse();
        child.SizeFraction.ShouldBe(0.5);
        neighbor.SizeFraction.ShouldBe(0.12);
    }

    [Fact]
    public void TryResizeChild_WhenDeltaIsZero_LeavesSizesUnchangedAndReturnsTrue()
    {
        var split = new SplitContainer();
        var child = new TilingWindow(new WindowId(1)) { SizeFraction = 0.5 };
        var neighbor = new TilingWindow(new WindowId(2)) { SizeFraction = 0.5 };
        split.AppendChild(child);
        split.AppendChild(neighbor);

        bool result = split.TryResizeChild(child, 0.0, neighbor);

        result.ShouldBeTrue();
        child.SizeFraction.ShouldBe(0.5);
        neighbor.SizeFraction.ShouldBe(0.5);
    }
}
