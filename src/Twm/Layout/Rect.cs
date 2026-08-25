namespace Twm.Layout;

/// <summary>Integer rectangle in virtual-screen coordinates.</summary>
public readonly record struct Rect(int X, int Y, int Width, int Height)
{
    public int Left => X;
    public int Top => Y;
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public static Rect FromLtrb(int left, int top, int right, int bottom) =>
        new(left, top, right - left, bottom - top);

    public override string ToString() => $"{X},{Y} {Width}x{Height}";
}
