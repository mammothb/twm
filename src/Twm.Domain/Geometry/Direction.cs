namespace Twm.Domain.Geometry;

/// <summary>A cardinal direction for focus and move commands.</summary>
public enum Direction
{
    Left,
    Right,
    Up,
    Down,
}

/// <summary>Helpers for <see cref="Direction" />.</summary>
public static class DirectionExtensions
{
    /// <summary>The tiling axis the direction travels along.</summary>
    public static TilingDirection Axis(this Direction direction)
    {
        return direction switch
        {
            Direction.Left or Direction.Right => TilingDirection.Horizontal,
            Direction.Up or Direction.Down => TilingDirection.Vertical,
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };
    }

    /// <summary>The direction pointing to opposite way.</summary>
    public static Direction Opposite(this Direction direction)
    {
        return direction switch
        {
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            _ => throw new ArgumentOutOfRangeException(nameof(direction)),
        };
    }
}
