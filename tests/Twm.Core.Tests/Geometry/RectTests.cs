using Twm.Core.Geometry;

namespace Twm.Core.Tests.Geometry;

public class RectTests
{
    [Fact]
    public void Rect_DerivesRightAndBottom()
    {
        var rect = new Rect(10, 20, 100, 50);

        rect.Right.ShouldBe(110);
        rect.Bottom.ShouldBe(70);
    }

    [Fact]
    public void Center_IsMidpoint()
    {
        var rect = new Rect(0, 0, 100, 50);

        rect.Center.ShouldBe(new Point(50, 25));
    }

    [Theory]
    [InlineData(0, 0, true)] // top-left is inclusive
    [InlineData(99, 49, true)] // inside near the far corner
    [InlineData(100, 25, false)] // right edge is exclusive
    [InlineData(25, 50, false)] // bottom edge is exclusive
    [InlineData(-1, 0, false)]
    public void Contains_RespectsHalfOpenBounds(int px, int py, bool expected)
    {
        var rect = new Rect(0, 0, 100, 50);

        rect.Contains(new Point(px, py)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(TilingDirection.Horizontal, 2)]
    [InlineData(TilingDirection.Horizontal, 3)]
    [InlineData(TilingDirection.Vertical, 2)]
    [InlineData(TilingDirection.Vertical, 4)]
    public void SplitEvenly_TilesParentExactly(TilingDirection direction, int count)
    {
        var parent = new Rect(10, 20, 101, 77);

        Rect[] slices = parent.SplitEvenly(direction, count);

        slices.Length.ShouldBe(count);
        AssertContiguousTiling(parent, direction, slices);
    }

    [Fact]
    public void SplitEvenly_PutsRemainderInLastSlice()
    {
        var parent = new Rect(0, 0, 100, 10);

        Rect[] slices = parent.SplitEvenly(TilingDirection.Horizontal, 3);

        slices[0].Width.ShouldBe(33);
        slices[1].Width.ShouldBe(33);
        slices[2].Width.ShouldBe(34);
    }

    [Fact]
    public void SplitEvenly_WithCountOne_ReturnsSameRect()
    {
        var parent = new Rect(5, 6, 7, 8);

        Rect[] slices = parent.SplitEvenly(TilingDirection.Vertical, 1);

        slices.Length.ShouldBe(1);
        slices[0].ShouldBe(parent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void SplitEvenly_RejectsNonPositiveCount(int count)
    {
        var parent = new Rect(0, 0, 100, 100);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            parent.SplitEvenly(TilingDirection.Horizontal, count)
        );
    }

    [Fact]
    public void SplitByWeights_ProportionsAndTiles_Exactly()
    {
        var parent = new Rect(0, 0, 200, 100);

        Rect[] slices = parent.Split(TilingDirection.Horizontal, [0.25, 0.75]);

        slices[0].Width.ShouldBe(50);
        slices[1].Width.ShouldBe(150);
        AssertContiguousTiling(parent, TilingDirection.Horizontal, slices);
    }

    [Fact]
    public void SplitByWeights_SendsRoundingRemainder_ToLastSlice()
    {
        var parent = new Rect(0, 0, 100, 10);

        Rect[] slices = parent.Split(TilingDirection.Horizontal, [1, 1, 1]);

        // 100 * 1/3 floors to 33 for the first two; the remainder lands on the last.
        slices[0].Width.ShouldBe(33);
        slices[1].Width.ShouldBe(33);
        slices[2].Width.ShouldBe(34);
    }

    [Fact]
    public void SplitByWeights_RejectsEmpty()
    {
        var parent = new Rect(0, 0, 100, 100);

        Should.Throw<ArgumentException>(() => parent.Split(TilingDirection.Horizontal, []));
    }

    [Fact]
    public void SplitByWeights_RejectsNonPositiveSum()
    {
        var parent = new Rect(0, 0, 100, 100);

        Should.Throw<ArgumentException>(() => parent.Split(TilingDirection.Vertical, [0, 0]));
    }

    [Fact]
    public void SplitByWeights_RejectsNegativeWeight()
    {
        var parent = new Rect(0, 0, 100, 100);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            parent.Split(TilingDirection.Vertical, [-1, 2])
        );
    }

    private static void AssertContiguousTiling(
        Rect parent,
        TilingDirection direction,
        Rect[] slices
    )
    {
        if (direction == TilingDirection.Horizontal)
        {
            slices[0].X.ShouldBe(parent.X);
            slices[^1].Right.ShouldBe(parent.Right);
            for (int i = 0; i < slices.Length; i++)
            {
                slices[i].Y.ShouldBe(parent.Y);
                slices[i].Height.ShouldBe(parent.Height);
                if (i > 0)
                {
                    slices[i].X.ShouldBe(slices[i - 1].Right);
                }
            }
        }
        else
        {
            slices[0].Y.ShouldBe(parent.Y);
            slices[^1].Bottom.ShouldBe(parent.Bottom);
            for (int i = 0; i < slices.Length; i++)
            {
                slices[i].X.ShouldBe(parent.X);
                slices[i].Width.ShouldBe(parent.Width);
                if (i > 0)
                {
                    slices[i].Y.ShouldBe(slices[i - 1].Bottom);
                }
            }
        }
    }
}
