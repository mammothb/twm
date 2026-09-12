using Twm.Domain.Tree;

namespace Twm.Domain.Tests.Tree;

public class MoveChildToIndexTests
{
    [Fact]
    public void MoveChildToIndex_WhenChildIsNull_ThrowsArgumentNullException()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        split.AppendChild(w1);

        Should.Throw<ArgumentNullException>(() => split.MoveChildToIndex(null!, 0));
    }

    [Fact]
    public void MoveChildToIndex_WhenChildIsNotAttached_ThrowsInvalidOperationException()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        var orphan = new TilingWindow(new WindowId(2));

        Should.Throw<InvalidOperationException>(() => split.MoveChildToIndex(orphan, 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void MoveChildToIndex_WhenNewIndexIsNegative_ThrowsArgumentOutOfRangeException(
        int newIndex
    )
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        var w3 = new TilingWindow(new WindowId(3));
        split.AppendChild(w1);
        split.AppendChild(w2);
        split.AppendChild(w3);

        Should.Throw<ArgumentOutOfRangeException>(() => split.MoveChildToIndex(w1, newIndex));
    }

    [Fact]
    public void MoveChildToIndex_WhenNewIndexEqualsChildCount_ThrowsArgumentOutOfRangeException()
    {
        // MoveChildToIndex uses ThrowIfGreaterThanOrEqual, so index == count
        // is out of range. InsertChild allows it; moving doesn't.
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        var w3 = new TilingWindow(new WindowId(3));
        split.AppendChild(w1);
        split.AppendChild(w2);
        split.AppendChild(w3);

        Should.Throw<ArgumentOutOfRangeException>(() => split.MoveChildToIndex(w1, 3));
    }

    [Fact]
    public void MoveChildToIndex_WhenNewIndexExceedsChildCount_ThrowsArgumentOutOfRangeException()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        split.AppendChild(w1);
        split.AppendChild(w2);

        Should.Throw<ArgumentOutOfRangeException>(() => split.MoveChildToIndex(w1, 100));
    }

    [Fact]
    public void MoveChildToIndex_WhenRejected_LeavesTreeUnchanged()
    {
        // Defensive contract: a rejected MoveChildToIndex must not partially
        // mutate the children list. We exercise a rejection path and assert
        // the order is exactly what we set up.
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        var w3 = new TilingWindow(new WindowId(3));
        split.AppendChild(w1);
        split.AppendChild(w2);
        split.AppendChild(w3);

        Should.Throw<ArgumentOutOfRangeException>(() => split.MoveChildToIndex(w1, 100));

        split.Children.ShouldBe([w1, w2, w3]);
    }
}
