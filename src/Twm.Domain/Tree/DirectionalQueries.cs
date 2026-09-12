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

    /// <summary>
    /// The window to focus when entering <paramref name="container" /> while
    /// travelling in <paramref name="moveDirection" />. Descends the tree: a
    /// real split is entered at its edge child along the travel axis, and by
    /// the child nearest <paramref name="fromCenter" /> on the perpendicular
    /// axis tabbed/stacked children overlap and have no spatial edge, so the
    /// most-recently-focused child is kept. Null if the container yields no
    /// tiling window (empty, or not a window/split container).
    /// </summary>
    private static TilingWindow? EntryWindow(
        this Container container,
        Direction moveDirection,
        Point fromCenter
    )
    {
        ArgumentNullException.ThrowIfNull(container);
        Container? node = container;
        while (node is not null)
        {
            if (node is TilingWindow window)
            {
                return window;
            }

            if (node is SplitContainer split)
            {
                node = EntryChild(split, moveDirection, fromCenter);
                continue;
            }

            return null;
        }

        return null;
    }

    private static Container? EntryChild(
        SplitContainer split,
        Direction moveDirection,
        Point fromCenter
    )
    {
        if (split.Children.Count == 0)
        {
            return null;
        }

        // Tabbed/stacked children overlap: there is no spatial edge, so honor
        // the remembered focus instead of layout position
        if (!split.Layout.IsSplit())
        {
            return split.LastFocusedChild;
        }

        // A real split is spatially meaningful only along its own axis: enter
        // from the edge opposite trave (moving Right/Down enters the first
        // child, Left/Up enters the last).
        if (split.Layout.Axis() == moveDirection.Axis())
        {
            return moveDirection is Direction.Right or Direction.Down
                ? split.Children[0]
                : split.Children[^1];
        }

        // Perpendicular split: keep spatial continuity by the child nearest
        // fromCenter on the perpendicular axis
        Container best = split.Children[0];
        long bestDelta = long.MaxValue;
        foreach (Container child in split.Children)
        {
            long delta = moveDirection is Direction.Left or Direction.Right
                ? Math.Abs(child.Bounds.Center.Y - fromCenter.Y)
                : Math.Abs(child.Bounds.Center.X - fromCenter.X);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                best = child;
            }
        }

        return best;
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
            .FindAncestor<Monitor>()
            ?.FindAdjacentMonitor(direction)
            ?.ActiveWorkspace;
        return activeWorkspace is null
            ? null
            : EntryWindow(activeWorkspace, direction, subject.Bounds.Center) ?? activeWorkspace;
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
