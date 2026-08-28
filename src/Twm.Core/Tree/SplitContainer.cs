namespace Twm.Core.Tree;

/// <summary>
/// An inner node that arranges its children according to a <see cref="LayoutMode" />
/// (split horizontal/vertical, tabbed, or stacked).
/// </summary>
public class SplitContainer(LayoutMode layout = LayoutMode.SplitHorizontal) : Container
{
    /// <summary>How this container arranges its childre.</summary>
    public LayoutMode Layout { get; set; } = layout;
}
