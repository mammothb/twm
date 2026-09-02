using Twm.Domain.Geometry;

namespace Twm.Domain.Tests.Geometry;

public class DirectionTests
{
    [Theory]
    [InlineData(Direction.Left, TilingDirection.Horizontal)]
    [InlineData(Direction.Right, TilingDirection.Horizontal)]
    [InlineData(Direction.Up, TilingDirection.Vertical)]
    [InlineData(Direction.Down, TilingDirection.Vertical)]
    public void Axis_MapsDirectionToTilingAxis(Direction direction, TilingDirection expected)
    {
        direction.Axis().ShouldBe(expected);
    }

    [Theory]
    [InlineData(Direction.Left, Direction.Right)]
    [InlineData(Direction.Right, Direction.Left)]
    [InlineData(Direction.Up, Direction.Down)]
    [InlineData(Direction.Down, Direction.Up)]
    public void Opposite_InvertsDirection(Direction direction, Direction expected)
    {
        direction.Opposite().ShouldBe(expected);
    }
}
