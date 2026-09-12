using Twm.Domain.Geometry;

namespace Twm.Domain.Tree;

/// <summary>A leaf container wrapping a single managed OS window.</summary>
public sealed class TilingWindow(WindowId windowId, WindowId? owner = null) : Container
{
    /// <summary>The identity of the wrapped OS window.</summary>
    public WindowId WindowId { get; } = windowId;

    /// <summary>
    /// The id of this window's owner (GW_OWNER on Windows), or null. The
    /// reconciler uses it to avoid cloaking a window that owns a visible one —
    /// DWM cloak cascades owner→owned, so cloaking an owner would hide its
    /// modal dialog too.
    /// </summary>
    public WindowId? Owner { get; } = owner;

    /// <summary>
    /// The container focus should move to when travelling in
    /// <paramref="direction" /> from <paramref="subject" />: the deepest
    /// focusable neighbor within the tree, or, at a workspace edge, the
    /// entry-edge window of the adjacent monitor's active workspace(falling
    /// back to that workspace itself). Null if there is nowhere to go).
    /// </summary>
    public Container? FindFocusTarget(Direction direction) =>
        FindInTree(direction) ?? FindCrossMonitor(direction);

    /// <summary>
    /// Whether this window should currently be shown on screen: it is on its
    /// monitor's active workspace <b>and</b>, through every tabbed/stacked
    /// ancestor up to the workspace, its branch is that containers's focused
    /// child (a non-focused tab is hidden). This is the single truth the
    /// reconciler shows/cloaks by, and the hide-event classifier reads.
    /// </summary>
    public bool IsEffectivelyVisible()
    {
        Workspace? workspace = FindAncestor<Workspace>();
        Container? activeWorkspace = workspace?.FindAncestor<Monitor>()?.LastFocusedChild;
        if (workspace is null || !ReferenceEquals(workspace, activeWorkspace))
        {
            return false;
        }

        // Walk window -> ... -> workspace (stops when the parent is the
        // Monitor). Any tabbed/stacked split on the path must have the branch
        // we came up through as its focused child.
        Container node = this;
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

    /// <summary>
    /// The window to focus when entering <paramref name="container" /> while
    /// travelling in <paramref name="moveDirection" />. Descends the tree: a
    /// real split is entered at its edge child along the travel axis, and by
    /// the child nearest <paramref name="fromCenter" /> on the perpendicular
    /// axis tabbed/stacked children overlap and have no spatial edge, so the
    /// most-recently-focused child is kept. Null if the container yields no
    /// tiling window (empty, or not a window/split container).
    /// </summary>
    private static TilingWindow? FindEntryWindow(
        Container container,
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
                node = FindEntryChild(split, moveDirection, fromCenter);
                continue;
            }

            return null;
        }

        return null;
    }

    private static Container? FindEntryChild(
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

    private Container? FindCrossMonitor(Direction direction)
    {
        Container? activeWorkspace = FindAncestor<Monitor>()
            ?.FindAdjacentMonitor(direction)
            ?.ActiveWorkspace;
        return activeWorkspace is null
            ? null
            : FindEntryWindow(activeWorkspace, direction, Bounds.Center) ?? activeWorkspace;
    }

    private Container? FindInTree(Direction direction)
    {
        TilingDirection axis = direction.Axis();
        int delta = direction is Direction.Left or Direction.Up ? -1 : 1;

        Container node = this;
        while (node.Parent is SplitContainer split)
        {
            if (split.Layout.Axis() == axis)
            {
                int neighborIndex = node.Index + delta;
                if (0 <= neighborIndex && neighborIndex < split.Children.Count)
                {
                    return split.Children[neighborIndex].LastFocusedDescendantOrSelf;
                }
            }

            node = split;
        }

        return null;
    }
}
