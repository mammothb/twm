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
}
