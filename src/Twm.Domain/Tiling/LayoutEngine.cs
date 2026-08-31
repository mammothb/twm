using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Domain.Tiling;

/// <summary>
/// Computes screen rectangles for a container tree. Pure and platform-free: it
/// only reads the tree and writes each container's
/// <see cref="Container.Bounds" />.
/// </summary>
public sealed class LayoutEngine(Gaps gaps, int titleBarHeight)
{
    /// <summary>
    /// Reserved strip per title row in a tabbed/stacked container (px).
    /// </summary>
    public const int DefaultTitleBarHeight = 24;

    private readonly Gaps _gaps = gaps;
    private readonly int _titleBarHeight = Math.Max(0, titleBarHeight);

    public LayoutEngine()
        : this(Gaps.None, DefaultTitleBarHeight) { }

    public LayoutEngine(Gaps gaps)
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

        if (split.Layout is Layout.Tabbed or Layout.Stacked)
        {
            // Reserve a title strip (tabbed = one row; stacked = one row per
            // child), then give every child the same content rect below it.
            // Only the focused child is shown (the reconciler cloaks the rest);
            // non-focused children still get valid bounds.
            int rows = split.Layout == Layout.Tabbed ? 1 : split.Children.Count;
            // Avoid content from being pushed to another container
            int strip = Math.Min(rect.Height, _titleBarHeight * rows);
            var content = new Rect(rect.X, rect.Y + strip, rect.Width, rect.Height - strip);
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

        // Divide full parent space without subtracting gaps
        bool isHorizontal = split.Layout.Axis() == TilingDirection.Horizontal;
        Rect probe = isHorizontal ? new Rect(0, 0, rect.Width, 1) : new Rect(0, 0, 1, rect.Height);
        Rect[] slices = probe.Split(split.Layout.Axis(), weights);

        Rect[] result = new Rect[count];
        int halfGap = innerGap / 2;
        for (int i = 0; i < count; i++)
        {
            int x = isHorizontal ? rect.X + slices[i].X : rect.X;
            int y = isHorizontal ? rect.Y : rect.Y + slices[i].Y;
            int width = isHorizontal ? slices[i].Width : rect.Width;
            int height = isHorizontal ? rect.Height : slices[i].Height;

            // Apply half-gaps to internal boundaries
            bool hasLeftOrTop = i > 0;
            bool hasRightOrBottom = i < count - 1;
            if (isHorizontal)
            {
                int insetLeft = hasLeftOrTop ? halfGap : 0;
                int insetRight = hasRightOrBottom ? halfGap : 0;
                x += insetLeft;
                width = Math.Max(0, width - insetLeft - insetRight);
            }
            else
            {
                int insetTop = hasLeftOrTop ? halfGap : 0;
                int insetBottom = hasRightOrBottom ? halfGap : 0;
                y += insetTop;
                height = Math.Max(0, height - insetTop - insetBottom);
            }
            result[i] = new Rect(x, y, width, height);
        }
        return result;
    }

    private static Rect Deflate(Rect rect, int amount) =>
        new(
            rect.X + amount,
            rect.Y + amount,
            Math.Max(0, rect.Width - (2 * amount)),
            Math.Max(0, rect.Height - (2 * amount))
        );
}
