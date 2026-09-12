using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Domain.Tests.Geometry;

public class TilingDirectionTests
{
    [Theory]
    [InlineData(TilingDirection.Horizontal, Layout.SplitHorizontal)]
    [InlineData(TilingDirection.Vertical, Layout.SplitVertical)]
    public void SplitLayout_WhenCalled_ReturnsSplittingLayoutForDirection(
        TilingDirection direction,
        Layout expected
    ) => direction.SplitLayout().ShouldBe(expected);

    [Fact]
    public void SplitLayout_WhenCalledForAllDefinedValues_ProducesBothSplittingLayouts()
    {
        HashSet<Layout> layouts = [];
        foreach (TilingDirection d in Enum.GetValues<TilingDirection>())
        {
            layouts.Add(d.SplitLayout());
        }

        layouts.SetEquals([Layout.SplitHorizontal, Layout.SplitVertical]).ShouldBeTrue();
    }

    [Theory]
    [InlineData(TilingDirection.Horizontal)]
    [InlineData(TilingDirection.Vertical)]
    public void SplitLayout_WhenComposedWithAxis_ReturnsOriginalDirection(
        TilingDirection direction
    ) =>
        // SplitLayout(d).Axis() == d for splitting layouts. Note this is NOT a
        // roundtrip for Tabbed / Stacked: their axis maps to Horizontal /
        // Vertical but SplitLayout only produces splits. Documented in
        // SplitLayout's behavior.
        direction.SplitLayout().Axis().ShouldBe(direction);
}
