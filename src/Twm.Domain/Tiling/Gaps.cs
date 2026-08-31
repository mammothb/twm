namespace Twm.Domain.Tiling;

/// <summary>
/// Gap sizes in pixels. <see cref="Outer" /> insets the workspace from the
/// monitor edges; <see cref="Inner" /> separates adjacent tiled windows.
/// </summary>
public readonly record struct Gaps(int Inner, int Outer)
{
    /// <summary>No gaps.</summary>
    public static Gaps None => new(0, 0);
}
