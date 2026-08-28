using Twm.Core.Geometry;

namespace Twm.Core.Tree;

/// <summary>
/// How a <see cref="SplitContainer" /> arranges its childre, i3's four `layout`
/// values. Split layouts tile children side by side along an axis; tabbed/stacked
/// show only the focused child at fulll size and collapse the rest behind a bar.
/// </summary>
public enum LayoutMode
{
    SplitHorizontal,
    SplitVertical,
    Tabbed,
    Stacked,
}

/// <summary>Helpers for <see cref="LayoutMode" />.</summary>
public static class LayoutModeExtensions
{
    /// <summary>
    /// The geometric axis focus and movement travel along: horizontal for
    /// split-horizontal and tabbed (childre run left-to-right), vertical for
    /// split-vertical and stacked.
    /// </summary>
    public static TilingDirection Axis(this LayoutMode layout)
    {
        return layout switch
        {
            LayoutMode.SplitVertical or LayoutMode.Stacked => TilingDirection.Vertical,
            _ => TilingDirection.Horizontal,
        };
    }

    /// <summary>
    /// Whether this is a side-by-side tiling layout (not tabbed/stacked).
    /// </summary>
    public static bool IsSplit(this LayoutMode layout)
    {
        return layout is LayoutMode.SplitHorizontal or LayoutMode.SplitVertical;
    }
}
