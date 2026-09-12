using Twm.Domain.Geometry;

namespace Twm.Domain.Tests.Geometry;

public class DirectionTests
{
    [Theory]
    [InlineData(Direction.Left, TilingDirection.Horizontal)]
    [InlineData(Direction.Right, TilingDirection.Horizontal)]
    [InlineData(Direction.Up, TilingDirection.Vertical)]
    [InlineData(Direction.Down, TilingDirection.Vertical)]
    public void Axis_WhenCalled_ReturnsTilingAxisForDirection(
        Direction direction,
        TilingDirection expected
    ) => direction.Axis().ShouldBe(expected);

    [Fact]
    public void Axis_WhenCalledForAllDefinedValues_CoversBothAxes()
    {
        HashSet<TilingDirection> axes = [];
        foreach (Direction d in Enum.GetValues<Direction>())
        {
            axes.Add(d.Axis());
        }

        axes.SetEquals([TilingDirection.Horizontal, TilingDirection.Vertical]).ShouldBeTrue();
    }

    [Fact]
    public void Axis_WhenGivenUndefinedValue_ThrowsArgumentOutOfRangeException() =>
        Should.Throw<ArgumentOutOfRangeException>(() => ((Direction)999).Axis());

    [Theory]
    [InlineData(Direction.Left, Direction.Right)]
    [InlineData(Direction.Right, Direction.Left)]
    [InlineData(Direction.Up, Direction.Down)]
    [InlineData(Direction.Down, Direction.Up)]
    public void Opposite_WhenCalled_ReturnsOppositeDirection(
        Direction direction,
        Direction expected
    ) => direction.Opposite().ShouldBe(expected);

    [Theory]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    public void Opposite_WhenAppliedTwice_ReturnsOriginalDirection(Direction direction) =>
        direction.Opposite().Opposite().ShouldBe(direction);

    [Theory]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    public void Opposite_WhenApplied_ReturnsDifferentDirection(Direction direction) =>
        direction.Opposite().ShouldNotBe(direction);

    [Theory]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Down)]
    public void Opposite_WhenApplied_PreservesAxis(Direction direction) =>
        direction.Opposite().Axis().ShouldBe(direction.Axis());

    [Fact]
    public void Opposite_WhenGivenUndefinedValue_ThrowsArgumentOutOfRangeException() =>
        Should.Throw<ArgumentOutOfRangeException>(() => ((Direction)999).Opposite());
}
