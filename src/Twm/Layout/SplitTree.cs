using Twm.Layout;

namespace Twm.Layout;

/// <summary>
/// One monitor's tiling tree. Owns insert/remove/focus/move/resize
/// semantics; geometry is computed by <see cref="Apply"/>.
/// </summary>
public sealed class SplitTree
{
    /// <summary>Minimum share of a split container any child may hold.</summary>
    public const double MinWeightShare = 0.05;

    private const long DirectionScoreScale = 1 << 16;

    public LayoutNode? Root { get; private set; }

    /// <summary>Focused leaf. Synced with real focus by the WindowManager.</summary>
    public WindowLeaf? Focused { get; private set; }

    public int Count { get; private set; }

    public IEnumerable<WindowLeaf> Leaves() => Root?.Leaves() ?? [];

    public WindowLeaf? Find(nint hwnd) => Leaves().FirstOrDefault(l => l.Hwnd == hwnd);

    /// <summary>External focus sync (foreground events, tests).</summary>
    public void SetFocused(WindowLeaf? leaf) => Focused = leaf;

    /// <summary>
    /// Adopts an externally built tree. Used by tests and, later, by
    /// restart-in-place state restoration.
    /// </summary>
    public static SplitTree Adopt(LayoutNode root, WindowLeaf? focused)
    {
        var tree = new SplitTree
        {
            Count = root.Leaves().Count(),
            Root = root,
            Focused = focused ?? root.Leaves().FirstOrDefault(),
        };
        return tree;
    }

    /// <summary>
    /// Inserts a new leaf next to the focused one (i3-style: becomes a
    /// sibling in the focused window's parent container). The new leaf
    /// takes half the anchor's weight so it opens at the anchor's size.
    /// </summary>
    public WindowLeaf Insert(nint hwnd)
    {
        var leaf = new WindowLeaf { Hwnd = hwnd };

        if (Root is null)
        {
            Root = leaf;
        }
        else
        {
            WindowLeaf anchor = Focused ?? Root.Leaves().Last();
            SplitContainer? parent = anchor.Parent;

            if (parent is null)
            {
                // Anchor is the sole root leaf: wrap it in the first split.
                var container = new SplitContainer { Horizontal = true };
                container.Add(anchor);
                container.Add(leaf);
                Root = container;
                leaf.Weight = 1.0;
            }
            else
            {
                int index = parent.IndexOf(anchor) + 1;
                leaf.Weight = 0.0; // set below, half of anchor's
                parent.InsertAt(index, leaf);
                leaf.Weight = anchor.Weight / 2.0;
                anchor.Weight /= 2.0;
            }
        }

        Count++;
        Focused = leaf;
        return leaf;
    }

    /// <summary>
    /// Removes a leaf, collapsing containers left with a single child.
    /// Focus falls to the nearest former neighbor if the removed window
    /// was focused.
    /// </summary>
    public void Remove(WindowLeaf leaf)
    {
        Count--;

        SplitContainer? parent = leaf.Parent;
        if (parent is null)
        {
            // Sole root leaf.
            Root = null;
            Focused = null;
            return;
        }

        WindowLeaf? refocus = ReferenceEquals(Focused, leaf) ? OrderNeighbor(leaf) : null;
        parent.Remove(leaf);
        Collapse(parent);
        if (ReferenceEquals(Focused, leaf))
        {
            Focused = refocus;
        }
    }

    /// <summary>
    /// Assigns rects to every node by dividing <paramref name="area"/>
    /// proportionally to child weights. Cumulative integer boundaries
    /// guarantee tiles exactly tile the area — no gaps, no overlaps,
    /// no rounding drift.
    /// </summary>
    public void Apply(Rect area)
    {
        if (Root is not null)
        {
            Assign(Root, area);
        }
    }

    private static void Assign(LayoutNode node, Rect rect)
    {
        node.AssignedRect = rect;
        if (node is not SplitContainer container || container.Children.Count == 0)
        {
            return;
        }

        double total = container.Children.Sum(c => Math.Max(c.Weight, MinWeightShare));
        int origin = container.Horizontal ? rect.X : rect.Y;
        int extent = container.Horizontal ? rect.Width : rect.Height;
        int previousBoundary = origin;
        double cumulativeWeight = 0;

        for (int i = 0; i < container.Children.Count; i++)
        {
            LayoutNode child = container.Children[i];
            cumulativeWeight += Math.Max(child.Weight, MinWeightShare);

            int boundary =
                i == container.Children.Count - 1
                    ? origin + extent // last child absorbs rounding remainder
                    : origin + (int)Math.Round(extent * cumulativeWeight / total);

            Rect childRect = container.Horizontal
                ? Rect.FromLtrb(previousBoundary, rect.Top, boundary, rect.Bottom)
                : Rect.FromLtrb(rect.Left, previousBoundary, rect.Right, boundary);

            Assign(child, childRect);
            previousBoundary = boundary;
        }
    }

    /// <summary>
    /// Nearest leaf in the given direction from the focused leaf, scored
    /// by axis distance first and perpendicular overlap second. Returns
    /// null when nothing lies that way. Also used by <see cref="MoveFocused"/>.
    /// </summary>
    public WindowLeaf? Neighbor(Direction direction)
    {
        if (Focused is null)
        {
            return null;
        }

        Rect focus = Focused.AssignedRect;
        WindowLeaf? best = null;
        long bestScore = long.MaxValue;

        foreach (WindowLeaf candidate in Leaves())
        {
            if (ReferenceEquals(candidate, Focused))
            {
                continue;
            }

            Rect r = candidate.AssignedRect;
            long primary;
            long overlap;

            switch (direction)
            {
                case Direction.Left:
                    if (r.Right > focus.X)
                    {
                        continue;
                    }

                    primary = focus.X - r.Right;
                    overlap = VerticalOverlap(focus, r);
                    break;
                case Direction.Right:
                    if (r.X < focus.Right)
                    {
                        continue;
                    }

                    primary = r.X - focus.Right;
                    overlap = VerticalOverlap(focus, r);
                    break;
                case Direction.Up:
                    if (r.Bottom > focus.Y)
                    {
                        continue;
                    }

                    primary = focus.Y - r.Bottom;
                    overlap = HorizontalOverlap(focus, r);
                    break;
                case Direction.Down:
                    if (r.Y < focus.Bottom)
                    {
                        continue;
                    }

                    primary = r.Y - focus.Bottom;
                    overlap = HorizontalOverlap(focus, r);
                    break;
                default:
                    continue;
            }

            long score =
                (primary * DirectionScoreScale)
                + (DirectionScoreScale - Math.Min(overlap, DirectionScoreScale));
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    public bool FocusDirection(Direction direction)
    {
        WindowLeaf? neighbor = Neighbor(direction);
        if (neighbor is null)
        {
            return false;
        }

        Focused = neighbor;
        return true;
    }

    /// <summary>Swaps the focused leaf with its directional neighbor.</summary>
    public bool MoveFocused(Direction direction)
    {
        WindowLeaf? target = Neighbor(direction);
        if (target is null || Focused is null)
        {
            return false;
        }

        Swap(Focused, target);
        return true;
    }

    private static void Swap(WindowLeaf a, WindowLeaf b)
    {
        SplitContainer pa = a.Parent!;
        SplitContainer pb = b.Parent!;
        int indexA = pa.IndexOf(a);
        int indexB = pb.IndexOf(b);

        pa.SetChild(indexA, b);
        pb.SetChild(indexB, a);
        a.Parent = pb;
        b.Parent = pa;
        // Weights travel with their windows: each keeps its preferred size.
    }

    /// <summary>
    /// Grows the focused window toward <paramref name="direction"/> by
    /// roughly <paramref name="pixelDelta"/>, by shifting weights on the
    /// nearest ancestor split oriented along that axis which has a
    /// sibling on the relevant side.
    /// </summary>
    public bool ResizeFocused(Direction direction, int pixelDelta)
    {
        if (Focused is null || pixelDelta == 0)
        {
            return false;
        }

        bool horizontal = direction.IsHorizontal();
        bool positive = direction is Direction.Right or Direction.Down;

        for (
            SplitContainer? container = Focused.Parent;
            container is not null;
            container = container.Parent
        )
        {
            if (container.Horizontal != horizontal)
            {
                continue;
            }

            int index = IndexOfChildContaining(container, Focused);
            int other = positive ? index + 1 : index - 1;
            if (other < 0 || other >= container.Children.Count)
            {
                continue; // no border this way; try the next ancestor
            }

            // The focused window always grows toward the direction;
            // the neighbor on that side yields the space.
            AdjustWeights(container, index, other, pixelDelta);
            return true;
        }

        return false;
    }

    private static void AdjustWeights(
        SplitContainer container,
        int growIndex,
        int shrinkIndex,
        int pixelDelta
    )
    {
        Rect rect = container.AssignedRect;
        int extent = container.Horizontal ? rect.Width : rect.Height;
        if (extent <= 0 || growIndex == shrinkIndex)
        {
            return;
        }

        double total = container.Children.Sum(c => Math.Max(c.Weight, MinWeightShare));
        double delta = (double)pixelDelta / extent * total;

        double grow = Math.Max(container.Children[growIndex].Weight, MinWeightShare) + delta;
        double shrink = Math.Max(container.Children[shrinkIndex].Weight, MinWeightShare) - delta;

        double min = total * MinWeightShare;
        if (grow < min)
        {
            shrink -= min - grow;
            grow = min;
        }
        if (shrink < min)
        {
            grow -= min - shrink;
            shrink = min;
        }

        container.Children[growIndex].Weight = grow;
        container.Children[shrinkIndex].Weight = shrink;
    }

    /// <summary>Flips the focused window's immediate parent container orientation.</summary>
    public bool ToggleOrientation()
    {
        SplitContainer? container = Focused?.Parent ?? Root as SplitContainer;
        if (container is null)
        {
            return false;
        }

        container.Horizontal = !container.Horizontal;
        return true;
    }

    private static int IndexOfChildContaining(SplitContainer container, LayoutNode inner)
    {
        for (int i = 0; i < container.Children.Count; i++)
        {
            if (LayoutNode.Contains(container.Children[i], inner))
            {
                return i;
            }
        }

        throw new InvalidOperationException("Focused leaf not found inside its own ancestor.");
    }

    private void Collapse(SplitContainer node)
    {
        while (node.Children.Count == 1)
        {
            LayoutNode onlyChild = node.Children[0];
            SplitContainer? grandparent = node.Parent;
            if (grandparent is null)
            {
                Root = onlyChild;
                onlyChild.Parent = null;
                return;
            }
            grandparent.Replace(node, onlyChild);
            node = grandparent;
        }
    }

    /// <summary>Previous or next leaf in document order — used as refocus fallback.</summary>
    private WindowLeaf? OrderNeighbor(WindowLeaf leaf)
    {
        var leaves = Leaves().ToList();
        int index = leaves.IndexOf(leaf);
        if (index > 0)
        {
            return leaves[index - 1];
        }

        if (index < leaves.Count - 1)
        {
            return leaves[index + 1];
        }

        return null;
    }

    private static long VerticalOverlap(Rect a, Rect b) =>
        Math.Max(0, Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top));

    private static long HorizontalOverlap(Rect a, Rect b) =>
        Math.Max(0, Math.Min(a.Right, b.Right) - Math.Max(a.Left, b.Left));
}
