namespace Twm.Domain.Tree;

/// <summary>
/// An inner node that arranges its children according to a
/// <see cref="Layout" /> (split horizontal/vertical, tabbed, or stacked).
/// </summary>
public class SplitContainer(Layout layout = Layout.SplitHorizontal) : Container
{
    /// <summary>How this container arranges its childre.</summary>
    public Layout Layout { get; set; } = layout;
}
