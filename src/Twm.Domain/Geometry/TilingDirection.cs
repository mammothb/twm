using Twm.Domain.Tree;

namespace Twm.Domain.Geometry;

/// <summary>
/// The axis along which a split container lays its children out.
/// </summary>
public enum TilingDirection
{
    Horizontal,
    Vertical,
}

/// <summary>Helpers for <see cref="TilingDirection" />.</summary>
public static class TilingDirectionExtensions
{
    public static Layout SplitLayout(this TilingDirection direction) =>
        direction == TilingDirection.Vertical ? Layout.SplitVertical : Layout.SplitHorizontal;
}
