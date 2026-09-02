using Twm.Domain.Geometry;

namespace Twm.Domain.Tree;

/// <summary> Directional, geometry-driven travesal over the tree, the queries
/// that back focus and move-by-direction commands. Distinct from the plain
/// lookups in <see cref="TreeQueries" />: these reason about container centers,
/// perpendicular overlap, and distance.
/// </summary>
public static class DirectionalQueries
{
    /// <summary>
    /// The nearest monitor lying in <paramref name="direction" /> from this
    /// one, by geometry (center beyond this monitor's center in that direction,
    /// with perpendicular overlap), or null if there is no monitor that way.
    /// </summary>
    public static Monitor? AdjacentMonitor(this Monitor monitor, Direction direction)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        if (monitor.Parent is not RootContainer root)
        {
            return null;
        }

        Rect from = monitor.Bounds;
        Monitor? nearest = null;
        int nearestDistance = int.MaxValue;
        foreach (Container child in root.Children)
        {
            if (child is not Monitor other || ReferenceEquals(other, monitor))
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

    /// <summary>
    /// The tiling window nearest the entry edge when crossing into this
    /// container moving in <paramref name="moveDirection" />, e.g., moving
    /// Right enters from the left, so the leftmost window. Ties on the entry
    /// axis are broken by the window nearest <paramref name="fromCenter" /> on
    /// the perpendicular axis, so focus lands in line with where it came from.
    /// Null if empty.
    /// </summary>
    public static TilingWindow? EdgeWindow(
        this Container container,
        Direction moveDirection,
        Point fromCenter
    )
    {
        ArgumentNullException.ThrowIfNull(container);
        TilingWindow? best = null;
        long bestPrimary = long.MaxValue;
        long bestSecondary = long.MaxValue;
        foreach (Container descendant in container.Descendants)
        {
            if (descendant is not TilingWindow window)
            {
                continue;
            }

            Rect bounds = window.Bounds;
            long primary = moveDirection switch
            {
                Direction.Right => bounds.X, // nearest left edge
                Direction.Left => -bounds.Right, // nearest right edge
                Direction.Down => bounds.Y, // nearest top edge
                Direction.Up => -bounds.Bottom, // nearest bottom edge
                _ => 0,
            };
            long secondary = moveDirection is Direction.Left or Direction.Right
                ? Math.Abs(bounds.Center.Y - fromCenter.Y)
                : Math.Abs(bounds.Center.X - fromCenter.X);

            if (primary < bestPrimary || (primary == bestPrimary && secondary < bestSecondary))
            {
                bestPrimary = primary;
                bestSecondary = secondary;
                best = window;
            }
        }
        return best;
    }

    /// <summary>
    /// The container focus should move to when travelling in
    /// <paramref="direction" /> from <paramref="subject" />: the deepest
    /// focusable neighbor within the tree, or, at a workspace edge, the
    /// entry-edge window of the adjacent monitor's active workspace(falling
    /// back to that workspace itself). Null if there is nowhere to go).
    /// </summary>
    public static Container? FocusTargetInDirection(this Container subject, Direction direction)
    {
        ArgumentNullException.ThrowIfNull(subject);
        return FindInTree(subject, direction) ?? CrossMonitorTarget(subject, direction);
    }

    private static Container? FindInTree(Container subject, Direction direction)
    {
        TilingDirection axis = direction.Axis();
        int delta = direction is Direction.Left or Direction.Up ? -1 : 1;

        Container node = subject;
        while (node.Parent is SplitContainer split)
        {
            if (split.Layout.Axis() == axis)
            {
                int neighborIndex = node.Index + delta;
                if (0 <= neighborIndex && neighborIndex < split.Children.Count)
                {
                    return DeepestFocusable(split.Children[neighborIndex]);
                }
            }
            node = split;
        }
        return null;
    }

    private static Container? CrossMonitorTarget(Container subject, Direction direction)
    {
        Container? activeWorkspace = subject
            .MonitorOf()
            ?.AdjacentMonitor(direction)
            ?.LastFocusedChild;

        return activeWorkspace?.EdgeWindow(direction, subject.Bounds.Center) ?? activeWorkspace;
    }

    private static Container DeepestFocusable(Container node)
    {
        if (node is TilingWindow)
        {
            return node;
        }
        return node.LastFocusedDescendant ?? node;
    }
}
