using Twm.Domain.Geometry;

namespace Twm.Domain.Tree;

/// <summary>
/// How a <see cref="SplitContainer" /> arranges its children, i3's four
/// `layout` values. Split layouts tile children side by side along an axis;
/// tabbed/stacked show only the focused child at fulll size and collapse the
/// rest behind a bar.
/// </summary>
public enum Layout
{
    SplitHorizontal,
    SplitVertical,
    Tabbed,
    Stacked,
}

/// <summary>Helpers for <see cref="Layout" />.</summary>
public static class LayoutExtensions
{
    /// <summary>
    /// The geometric axis focus and movement travel along: horizontal for
    /// split-horizontal and tabbed (children run left-to-right), vertical for
    /// split-vertical and stacked.
    /// </summary>
    public static TilingDirection Axis(this Layout layout)
    {
        return layout switch
        {
            Layout.SplitVertical or Layout.Stacked => TilingDirection.Vertical,
            _ => TilingDirection.Horizontal,
        };
    }

    /// <summary>
    /// Whether this is a side-by-side tiling layout (not tabbed/stacked).
    /// </summary>
    public static bool IsSplit(this Layout layout)
    {
        return layout is Layout.SplitHorizontal or Layout.SplitVertical;
    }
}
