using Twm.Domain.Geometry;

namespace Twm.Domain.Tree;

/// <summary>
/// Tree-restructuring operations, the i3 command <i>semantics</i> (split, move,
/// resize, adopt, remove) as pure mutations of the container tree. Mutates the
/// tree only; they never compute layout geometry or touch the OS. The caller
/// re-arranges (<c>LayoutEngine</c>) and the reconciler pushes the result to
/// the OS afterwards.
/// </summary>
public static class TreeMutations
{
    /// <summary>Smallest size fraction a container may be resized to.</summary>
    private const double MinimumFraction = 0.1;

    /// <summary>
    /// Walks up from <paramref name="start" /> removing empty splits and
    /// flattening single-child splits (the lone child takes the split's place
    /// and size). Never removes or flattens a workspace.
    /// </summary>
    public static void Cleanup(this Container? start)
    {
        Container? node = start;
        while (node is SplitContainer split and not Workspace && split.Parent is Container parent)
        {
            if (split.Children.Count == 0)
            {
                parent.RemoveChild(split);
                node = parent;
            }
            else if (split.Children.Count == 1)
            {
                Container onlyChild = split.Children[0];
                double fraction = split.SizeFraction;
                split.RemoveChild(onlyChild);
                onlyChild.SizeFraction = fraction;
                parent.ReplaceChild(split, onlyChild);
                node = parent;
            }
            else
            {
                return;
            }
        }
    }

    /// <summary>
    /// i3's <c>resize grow/shrink width/height</c>: walks up to the nearest
    /// ancestor split on the direction's axis and trades size between the
    /// subject's branch and its neighbor Right/Down grow, Left/Up shrink.
    /// Returns whether it applied.
    /// </summary>
    public static bool ResizeInDirection(
        this Container subject,
        Direction direction,
        double deltaFraction
    )
    {
        ArgumentNullException.ThrowIfNull(subject);
        TilingDirection axis = direction.Axis();
        bool grow = direction is Direction.Right or Direction.Down;
        double delta = grow ? deltaFraction : -deltaFraction;

        // Walk up to the nearest ancestor split on the matching axis whose
        // child on the subject's path has a neighbor to trade size with
        Container pivot = subject;
        while (pivot.Parent is SplitContainer split)
        {
            if (
                split.Layout.Axis() == axis
                && Trade(pivot, pivot.NextSibling ?? pivot.PreviousSibling, delta)
            )
            {
                return true;
            }
            pivot = split;
        }
        return false;
    }

    /// <summary>
    /// Grows <paramref name="subject" /> within its parent split by
    /// <paramref name="delta" />, taking the same from an adjacent sibling.
    /// Returns whether it applied (a neighbor exists and neither side falls
    /// below the minimum fraction).
    /// </summary>
    public static bool ResizeWithNeighbor(this Container subject, double delta)
    {
        ArgumentNullException.ThrowIfNull(subject);
        return Trade(subject, subject.NextSibling ?? subject.PreviousSibling, delta);
    }

    /// <summary>
    /// Adopts a new window into the workspace: next to the workspace's focused
    /// window or filling the workspace when empty. Returns the new window (not
    /// yet focused).
    /// </summary>
    public static TilingWindow Adopt(this Workspace workspace, WindowId windowId)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var window = new TilingWindow(windowId);

        // Open next to the workspace's focused window, i3-style; otherwise fill
        // the workspace
        if (
            workspace.LastFocusedDescendant is TilingWindow focused
            && focused.Parent is SplitContainer parent
        )
        {
            parent.InsertChild(focused.Index + 1, window);
        }
        else
        {
            workspace.AppendChild(window);
        }

        return window;
    }

    /// <summary>
    /// Moves the window one step in a direction withing its monitor,
    /// restructuring the tree i3-style (reorder within a split, dive into an
    /// adjacent split, or pop out beside an ancestor). Returns whether the tree
    /// changed.
    /// </summary>
    public static bool MoveInDirection(this TilingWindow subject, Direction direction)
    {
        ArgumentNullException.ThrowIfNull(subject);
        if (subject.Parent is not SplitContainer subjectParent)
        {
            return false;
        }

        TilingDirection axis = direction.Axis();
        int delta = direction is Direction.Left or Direction.Up ? -1 : 1;

        // Walk up to the nearest ancestor split whose orientation matches the
        // move axis. `pivot` is that split's child on the subject's path.
        Container pivot = subject;
        while (pivot.Parent is SplitContainer split)
        {
            if (split.Layout.Axis() == axis)
            {
                int neighborIndex = pivot.Index + delta;
                bool inBounds = 0 <= neighborIndex && neighborIndex < split.Children.Count;

                if (ReferenceEquals(pivot, subject))
                {
                    if (inBounds)
                    {
                        Container neighbor = split.Children[neighborIndex];
                        if (neighbor is SplitContainer nested && nested.Children.Count > 0)
                        {
                            // Move into the adjacent split at its near edge
                            subjectParent.RemoveChild(subject);
                            int insertAt = delta > 0 ? 0 : nested.Children.Count;
                            nested.InsertChild(insertAt, subject);
                            subjectParent.Cleanup();
                        }
                        else
                        {
                            // Reorder within the same split
                            split.MoveChildToIndex(subject, neighborIndex);
                        }

                        subject.Focus();
                        return true;
                    }
                }
                else if (inBounds)
                {
                    // Subject is nested deeper: pop it out beside its pivot
                    // branch
                    subjectParent.RemoveChild(subject);
                    int insertAt = delta > 0 ? pivot.Index + 1 : pivot.Index;
                    split.InsertChild(insertAt, subject);
                    subjectParent.Cleanup();
                    subject.Focus();
                    return true;
                }
            }
            pivot = split;
        }
        return false;
    }

    public static bool MoveToAdjacentMonitor(this TilingWindow subject, Direction direction)
    {
        ArgumentNullException.ThrowIfNull(subject);
        if (
            subject.MonitorOf()?.AdjacentMonitor(direction)?.LastFocusedChild
                is not SplitContainer targetWorkspace
            || subject.Parent is not Container oldParent
        )
        {
            return false;
        }

        int delta = direction is Direction.Left or Direction.Up ? -1 : 1;
        oldParent.RemoveChild(subject);
        int insertAt = delta > 0 ? 0 : targetWorkspace.Children.Count;
        targetWorkspace.InsertChild(insertAt, subject);
        oldParent.Cleanup();
        subject.Focus();
        return true;
    }

    /// <summary>
    /// Moves the window to <paramref name="target" />. Returns whether it
    /// moved.
    /// </summary>
    public static bool MoveToWorkspace(this TilingWindow subject, Workspace target)
    {
        ArgumentNullException.ThrowIfNull(subject);
        if (
            ReferenceEquals(subject.WorkspaceOf(), target)
            || subject.Parent is not Container oldParent
        )
        {
            return false;
        }

        oldParent.RemoveChild(subject);
        target.AppendChild(subject);
        oldParent.Cleanup();
        return true;
    }

    /// <summary>
    /// Detaches the window from the tree and prunes any split it emptied.
    /// </summary>
    public static void Remove(this TilingWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (window.Parent is not Container parent)
        {
            return;
        }
        parent.RemoveChild(window);
        parent.Cleanup();
    }

    /// <summary>
    /// i3's <c>split</c>: a lone window re-orients its parent split; otherwise
    /// the window is wrapped in a new split of the given direction so the next
    /// neighbor nests inside.
    /// </summary>
    public static void SplitInDirection(this TilingWindow subject, TilingDirection direction)
    {
        ArgumentNullException.ThrowIfNull(subject);
        if (subject.Parent is not SplitContainer parent)
        {
            return;
        }

        // A lone window: just set its parent split's direction (i3 splits a
        // solitary window by re-orienting its container rather than nesting a
        // redundant single-child split
        if (parent.Children.Count == 1)
        {
            parent.Layout = ToSplitLayout(direction);
            return;
        }

        // Otherwise wrap the focused window in a new split; the next window
        // inserted next to it will nest inside
        int index = subject.Index;
        double fraction = subject.SizeFraction;

        var wrapper = new SplitContainer(ToSplitLayout(direction));
        parent.RemoveChild(subject);
        subject.SizeFraction = 1.0;
        wrapper.AppendChild(subject);
        wrapper.SizeFraction = fraction;
        parent.InsertChild(index, wrapper);
        subject.Focus();
    }

    /// <summary>
    /// i3's <c>layout toggle split</c>: flip split-horizontal and
    /// split-vertical; from tabbed or stacked, exit to a horizontal split.
    /// </summary>
    public static void ToggleSplitDirection(this SplitContainer split)
    {
        ArgumentNullException.ThrowIfNull(split);
        split.Layout = split.Layout switch
        {
            Layout.SplitHorizontal => Layout.SplitVertical,
            Layout.SplitVertical => Layout.SplitHorizontal,
            _ => Layout.SplitHorizontal,
        };
    }

    private static Layout ToSplitLayout(TilingDirection direction) =>
        direction == TilingDirection.Vertical ? Layout.SplitVertical : Layout.SplitHorizontal;

    private static bool Trade(Container pivot, Container? neighbor, double delta)
    {
        if (neighbor is null)
        {
            return false;
        }

        double newPivot = pivot.SizeFraction + delta;
        double newNeighbor = neighbor.SizeFraction - delta;
        if (newPivot < MinimumFraction || newNeighbor < MinimumFraction)
        {
            return false;
        }

        pivot.SizeFraction = newPivot;
        neighbor.SizeFraction = newNeighbor;
        return true;
    }
}
