namespace Twm.Layout;

public enum Direction
{
    Left,
    Right,
    Up,
    Down,
}

public static class DirectionExtensions
{
    public static bool IsHorizontal(this Direction direction) =>
        direction is Direction.Left or Direction.Right;
}
