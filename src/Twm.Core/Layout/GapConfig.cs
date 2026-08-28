namespace Twm.Core.Layout;

/// <summary>
/// Gap sizes in pixels. <see cref="Outer" /> insets the workspace from the
/// monitor edges; <see cref="Inner" /> separates adjacent tiled windows.
/// </summary>
public readonly record struct GapConfig(int Inner, int Outer)
{
    /// <summary>No gaps.</summary>
    public static GapConfig None => new(0, 0);
}
