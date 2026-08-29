using Twm.Core.Geometry;

namespace Twm.Core.Tree;

/// <summary>Read-only queries over the container tree.</summary>
public static class TreeQueries
{
    /// <summary>The globally focused window, or null.</summary>
    public static TilingWindow? FocusedWindow(this RootContainer root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return root.LastFocusedDescendant as TilingWindow;
    }

    /// <summary>
    /// Finds a managed window by id anywhere under the container.
    /// </summary>
    public static TilingWindow? FindWindow(this Container container, WindowId id)
    {
        ArgumentNullException.ThrowIfNull(container);
        foreach (Container descendant in container.Descendants)
        {
            if (descendant is TilingWindow window && window.WindowId == id)
            {
                return window;
            }
        }
        return null;
    }

    /// <summary>
    /// Finds a workspace by name anywhere under the container.
    /// </summary>
    public static Workspace? FindWorkspace(this Container container, string name)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(name);
        foreach (Container descendant in container.Descendants)
        {
            if (descendant is Workspace workspace && workspace.Name == name)
            {
                return workspace;
            }
        }
        return null;
    }

    /// <summary>The monitor a container belongs to, or null if detached.</summary>
    public static Monitor? MonitorOf(this Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        foreach (Container ancestor in container.Ancestors)
        {
            if (ancestor is Monitor monitor)
            {
                return monitor;
            }
        }
        return null;
    }

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

    private static bool IsInDirection(Rect from, Rect to, Direction direction)
    {
        return direction switch
        {
            Direction.Left => to.Center.X < from.Center.X && VerticalOverlap(from, to),
            Direction.Right => to.Center.X > from.Center.X && VerticalOverlap(from, to),
            Direction.Up => to.Center.Y < from.Center.Y && HorizontalOverlap(from, to),
            Direction.Down => to.Center.Y > from.Center.Y && HorizontalOverlap(from, to),
            _ => false,
        };
    }

    private static bool VerticalOverlap(Rect a, Rect b)
    {
        return a.Y < b.Bottom && b.Y < a.Bottom;
    }

    private static bool HorizontalOverlap(Rect a, Rect b)
    {
        return a.X < b.Right && b.X < a.Right;
    }

    private static int DirectionalDistance(Rect from, Rect to, Direction direction)
    {
        return direction is Direction.Left or Direction.Right
            ? Math.Abs(to.Center.X - from.Center.X)
            : Math.Abs(to.Center.Y - from.Center.Y);
    }

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

    /// <summary>The workspace a container belongs to, or null if detached.</summary>
    public static Workspace? WorkspaceOf(this Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        foreach (Container ancestor in container.Ancestors)
        {
            if (ancestor is Workspace workspace)
            {
                return workspace;
            }
        }
        return null;
    }

    /// <summary>
    /// Whether this window should currently be shown on screen: it is on its
    /// monitor's active workspace <b>and</b>, through every tabbed/stacked
    /// ancestor up to the workspace, its branch is that containers's focused
    /// child (a non-focused tab is hidden). This is the single truth the
    /// reconciler shows/cloaks by, and the hide-event classifier reads.
    public static bool IsEffectivelyVisible(this TilingWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        Workspace? workspace = window.WorkspaceOf();
        Container? activeWorkspace = workspace?.MonitorOf()?.LastFocusedChild;
        if (workspace is null || !ReferenceEquals(workspace, activeWorkspace))
        {
            return false;
        }

        // Walk window -> ... -> workspace (stops when the parent is the
        // Monitor). Any tabbed/stacked split on the path must have the branch
        // we came up through as its focused child.
        Container node = window;
        while (node.Parent is SplitContainer split)
        {
            if (!split.Layout.IsSplit() && !ReferenceEquals(split.LastFocusedChild, node))
            {
                return false;
            }
            node = split;
        }
        return true;
    }
}
