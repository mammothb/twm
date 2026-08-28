using Twm.Core.Geometry;
using Twm.Core.Tree;

namespace Twm.Core.Layout;

/// <summary>
/// Computes screen rectangles for a container tree. Pure and platform-free: it
/// only reads the tree and writes each container's
/// <see cref="Container.Bounds" />.
/// </summary>
public sealed class LayoutEngine(GapConfig gaps, int titleBarHeight)
{
    /// <summary>
    /// Reserved strip per title row in a tabbed/stacked container (px).
    /// </summary>
    public const int DefaultTitleBarHeight = 24;

    private readonly GapConfig _gaps = gaps;
    private readonly int _titleBarHeight = titleBarHeight;

    public LayoutEngine()
        : this(GapConfig.None, DefaultTitleBarHeight) { }

    public LayoutEngine(GapConfig gaps)
        : this(gaps, DefaultTitleBarHeight) { }

    /// <summary>Arranges every monitor under the root.</summary>
    public void Arrange(RootContainer root)
    {
        ArgumentNullException.ThrowIfNull(root);
        foreach (Container child in root.Children)
        {
            if (child is Monitor monitor)
            {
                Arrange(monitor);
            }
        }
    }

    /// <summary>
    /// Arranges each workspace on the monitor to fill the monitor bounds inset
    /// by the outer gap, then lays out the workspace's split tree.
    /// </summary>
    public void Arrange(Monitor monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        Rect workspaceRect = Deflate(monitor.Bounds, _gaps.Outer);
        foreach (Container child in monitor.Children)
        {
            if (child is Workspace workspace)
            {
                ArrangeNode(workspace, workspaceRect);
            }
        }
    }

    private void ArrangeNode(Container node, Rect rect)
    {
        node.Bounds = rect;
        if (node is not SplitContainer split || split.Children.Count == 0)
        {
            return;
        }

        if (split.Layout is LayoutMode.Tabbed or LayoutMode.Stacked)
        {
            // Reserve a title strip (tabbed = one row; stacked = one row per
            // child), then give every child the same content rect below it.
            // Only the focused child is shown (the reconsiler cloaks the rest);
            // non-focused children still get valid bounds.
            int rows = split.Layout == LayoutMode.Tabbed ? 1 : split.Children.Count;
            int strip = _titleBarHeight * rows;
            var content = new Rect(
                rect.X,
                rect.Y + strip,
                rect.Width,
                Math.Max(0, rect.Height - strip)
            );
            foreach (Container child in split.Children)
            {
                ArrangeNode(child, content);
            }
            return;
        }

        Rect[] childRects = ComputeChildRects(split, rect, _gaps.Inner);
        for (int i = 0; i < split.Children.Count; i++)
        {
            ArrangeNode(split.Children[i], childRects[i]);
        }
    }

    private static Rect[] ComputeChildRects(SplitContainer split, Rect rect, int innerGap)
    {
        int count = split.Children.Count;
        double[] weights = new double[count];
        for (int i = 0; i < count; i++)
        {
            weights[i] = split.Children[i].SizeFraction;
        }

        bool isHorizontal = split.Layout.Axis() == TilingDirection.Horizontal;
        int axisLength = isHorizontal ? rect.Width : rect.Height;
        int totalGap = innerGap * (count - 1);
        int available = Math.Max(0, axisLength - totalGap);

        // Split the gap-reduced length by weights using rounding rule, then
        // position the slices with an inner gap between each.
        Rect probe = isHorizontal ? new Rect(0, 0, available, 1) : new Rect(0, 0, 1, available);
        Rect[] slices = probe.Split(split.Layout.Axis(), weights);

        Rect[] result = new Rect[count];
        int offset = isHorizontal ? rect.X : rect.Y;
        for (int i = 0; i < count; i++)
        {
            int size = isHorizontal ? slices[i].Width : slices[i].Height;
            result[i] = isHorizontal
                ? new Rect(offset, rect.Y, size, rect.Height)
                : new Rect(rect.X, offset, rect.Width, size);
            offset += size + innerGap;
        }
        return result;
    }

    private static Rect Deflate(Rect rect, int amount)
    {
        return new Rect(
            rect.X + amount,
            rect.Y + amount,
            Math.Max(0, rect.Width - (2 * amount)),
            Math.Max(0, rect.Height - (2 * amount))
        );
    }
}
