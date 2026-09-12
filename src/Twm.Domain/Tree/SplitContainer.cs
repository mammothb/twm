namespace Twm.Domain.Tree;

/// <summary>
/// An inner node that arranges its children according to a
/// <see cref="Layout" /> (split horizontal/vertical, tabbed, or stacked).
/// </summary>
public class SplitContainer(Layout layout = Layout.SplitHorizontal) : Container
{
    /// <summary>Smallest size fraction a container may be resized to.</summary>
    private const double MinimumFraction = 0.1;

    /// <summary>How this container arranges its childre.</summary>
    public Layout Layout { get; set; } = layout;

    /// <summary>
    /// i3's <c>layout toggle split</c>: flip split-horizontal and
    /// split-vertical; from tabbed or stacked, exit to a horizontal split.
    /// </summary>
    public void ToggleSplitDirection()
    {
        Layout = Layout switch
        {
            Layout.SplitHorizontal => Layout.SplitVertical,
            Layout.SplitVertical => Layout.SplitHorizontal,
            _ => Layout.SplitHorizontal,
        };
    }

    internal bool TryResizeChild(Container child, double delta, Container? neighbor)
    {
        if (neighbor is null)
        {
            return false;
        }

        double newChild = child.SizeFraction + delta;
        double newNeighbor = neighbor.SizeFraction - delta;
        if (newChild < MinimumFraction || newNeighbor < MinimumFraction)
        {
            return false;
        }

        child.SizeFraction = newChild;
        neighbor.SizeFraction = newNeighbor;
        return true;
    }
}
