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
    public void Axis_MapsLayoutToTilingAxis(Layout layout, TilingDirection expected) =>
        layout.Axis().ShouldBe(expected);

    [Fact]
    public void Axis_PartitionIsExhaustiveOverDefinedValues()
    {
        HashSet<TilingDirection> axes = [];
        foreach (Layout l in Enum.GetValues<Layout>())
        {
            axes.Add(l.Axis());
        }

        axes.SetEquals([TilingDirection.Horizontal, TilingDirection.Vertical]).ShouldBeTrue();
    }

    [Theory]
    [InlineData(Layout.SplitHorizontal, true)]
    [InlineData(Layout.SplitVertical, true)]
    [InlineData(Layout.Tabbed, false)]
    [InlineData(Layout.Stacked, false)]
    public void IsSplit_TrueForSideBySideLayouts(Layout layout, bool expected) =>
        layout.IsSplit().ShouldBe(expected);
}
