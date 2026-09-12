using Twm.Domain.Geometry;

namespace Twm.Domain.Tree;

/// <summary>
/// A physical display. Holds workspaces; only one workspace is shown at a time,
/// filling the display bounds.
/// </summary>
public sealed class Monitor : Container
{
    public Monitor(Rect displayBounds)
    {
        Bounds = displayBounds;
    }

    /// <summary>
    /// The monitor's active workspace, its most-recently-focused workspace, or
    /// its first workspace if none has been focused. Null if the monitor has no
    /// workspaces.
    /// </summary>
    public Workspace? ActiveWorkspace =>
        LastFocusedChild as Workspace ?? Children.OfType<Workspace>().FirstOrDefault();

    /// <summary>
    /// The nearest monitor lying in <paramref name="direction" /> from this
    /// one, by geometry (center beyond this monitor's center in that direction,
    /// with perpendicular overlap), or null if there is no monitor that way.
    /// </summary>
    public Monitor? FindAdjacentMonitor(Direction direction)
    {
        if (Parent is not RootContainer root)
        {
            return null;
        }

        Rect from = Bounds;
        Monitor? nearest = null;
        int nearestDistance = int.MaxValue;
        foreach (Container child in root.Children)
        {
            if (child is not Monitor other || ReferenceEquals(other, this))
            {
                continue;
            }

            Rect to = other.Bounds;
            if (!IsInDirection(from, to, direction))
            {
                continue;
            }

            int distance = DirectionalDistance(from, to, direction);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = other;
            }
        }

        return nearest;
    }

    private static int DirectionalDistance(Rect from, Rect to, Direction direction) =>
        direction is Direction.Left or Direction.Right
            ? Math.Abs(to.Center.X - from.Center.X)
            : Math.Abs(to.Center.Y - from.Center.Y);

    private static bool IsInDirection(Rect from, Rect to, Direction direction) =>
        direction switch
        {
            Direction.Left => to.Center.X < from.Center.X && VerticalOverlap(from, to),
            Direction.Right => to.Center.X > from.Center.X && VerticalOverlap(from, to),
            Direction.Up => to.Center.Y < from.Center.Y && HorizontalOverlap(from, to),
            Direction.Down => to.Center.Y > from.Center.Y && HorizontalOverlap(from, to),
            _ => false,
        };

    private static bool HorizontalOverlap(Rect a, Rect b) => a.X < b.Right && b.X < a.Right;

    private static bool VerticalOverlap(Rect a, Rect b) => a.Y < b.Bottom && b.Y < a.Bottom;
}
