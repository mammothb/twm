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
