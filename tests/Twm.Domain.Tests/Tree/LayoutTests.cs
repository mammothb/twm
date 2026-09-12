using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Domain.Tests.Tree;

public class LayoutTests
{
    [Theory]
    [InlineData(Layout.SplitHorizontal, TilingDirection.Horizontal)]
    [InlineData(Layout.SplitVertical, TilingDirection.Vertical)]
    [InlineData(Layout.Tabbed, TilingDirection.Horizontal)]
    [InlineData(Layout.Stacked, TilingDirection.Vertical)]
    public void Axis_WhenCalled_ReturnsTilingAxisForLayout(
        Layout layout,
        TilingDirection expected
    ) => layout.Axis().ShouldBe(expected);

    [Fact]
    public void Axis_WhenCalledForAllDefinedValues_CoversBothAxes()
    {
        HashSet<TilingDirection> axes = [];
        foreach (Layout l in Enum.GetValues<Layout>())
        {
            axes.Add(l.Axis());
        }

        axes.SetEquals([TilingDirection.Horizontal, TilingDirection.Vertical]).ShouldBeTrue();
    }

    [Theory]
    [InlineData(Layout.SplitHorizontal)]
    [InlineData(Layout.SplitVertical)]
    public void IsSplit_WhenLayoutIsSplitting_ReturnsTrue(Layout layout) =>
        layout.IsSplit().ShouldBe(true);

    [Theory]
    [InlineData(Layout.Tabbed)]
    [InlineData(Layout.Stacked)]
    public void IsSplit_WhenLayoutIsTabbedOrStacked_ReturnsFalse(Layout layout) =>
        layout.IsSplit().ShouldBe(false);
}
