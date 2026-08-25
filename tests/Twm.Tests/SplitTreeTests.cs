using Shouldly;
using Twm.Layout;
using Xunit;

namespace Twm.Tests;

public class SplitTreeTests
{
    private const int W = 1000;
    private const int H = 600;

    private static Rect Area => new(0, 0, W, H);

    private static SplitTree Tree(params nint[] hwnds)
    {
        var tree = new SplitTree();
        foreach (var h in hwnds)
            tree.Insert(h);
        tree.Apply(Area);
        return tree;
    }

    // ---------- insertion ----------

    [Fact]
    public void First_window_becomes_root_leaf_filling_area()
    {
        var tree = Tree(1);

        tree.Root.ShouldBeOfType<WindowLeaf>();
        tree.Count.ShouldBe(1);
        tree.Find(1)!.AssignedRect.ShouldBe(Area);
    }

    [Fact]
    public void Second_window_splits_horizontally_into_equal_halves()
    {
        var tree = Tree(1, 2);

        tree.Find(1)!.AssignedRect.ShouldBe(new Rect(0, 0, W / 2, H));
        tree.Find(2)!.AssignedRect.ShouldBe(Rect.FromLtrb(W / 2, 0, W, H));
    }

    [Fact]
    public void Third_window_halves_its_anchor_not_the_whole_row()
    {
        // i3: new window takes half of the focused (anchor) window's space.
        // Insert order 1,2,3 → focus on 2 when 3 arrives.
        var tree = Tree(1, 2, 3);

        tree.Find(1)!.AssignedRect.ShouldBe(new Rect(0, 0, 500, H));
        tree.Find(2)!.AssignedRect.ShouldBe(Rect.FromLtrb(500, 0, 750, H));
        tree.Find(3)!.AssignedRect.ShouldBe(Rect.FromLtrb(750, 0, W, H));
    }

    [Fact]
    public void New_window_anchors_at_focused_window()
    {
        var tree = Tree(1, 2);
        tree.SetFocused(tree.Find(1)!); // focus left leaf this time
        tree.Insert(3);
        tree.Apply(Area);

        tree.Find(1)!.AssignedRect.ShouldBe(new Rect(0, 0, 250, H));
        tree.Find(3)!.AssignedRect.ShouldBe(Rect.FromLtrb(250, 0, 500, H));
        tree.Find(2)!.AssignedRect.ShouldBe(Rect.FromLtrb(500, 0, W, H));
    }

    // ---------- geometry invariants ----------

    [Fact]
    public void Tiles_never_overlap_and_cover_the_area_exactly()
    {
        var tree = new SplitTree();
        foreach (var h in new nint[] { 1, 2, 3, 4, 5 })
            tree.Insert(h);

        var root = (SplitContainer)tree.Root!;
        root.Children[0].Weight = 2.5; // awkward weights to stress rounding
        root.Children[2].Weight = 0.3;

        var area = new Rect(-7, 13, 1023, 587);
        tree.Apply(area);

        VerifyTiling(root, area);
    }

    private static void VerifyTiling(SplitContainer container, Rect bounds)
    {
        container.AssignedRect.ShouldBe(bounds);

        var children = container.Children;
        for (int i = 1; i < children.Count; i++)
        {
            var prev = children[i - 1].AssignedRect;
            var cur = children[i].AssignedRect;
            if (container.Horizontal)
                prev.Right.ShouldBe(cur.Left, "tiles must be contiguous horizontally");
            else
                prev.Bottom.ShouldBe(cur.Top, "tiles must be contiguous vertically");
        }

        for (int i = 0; i < children.Count; i++)
        {
            Rect? childBounds =
                i == children.Count - 1
                    ? Rect.FromLtrb(
                        container.Horizontal ? children[i].AssignedRect.Left : bounds.Left,
                        container.Horizontal ? bounds.Top : children[i].AssignedRect.Top,
                        container.Horizontal ? bounds.Right : children[i].AssignedRect.Right,
                        container.Horizontal ? children[i].AssignedRect.Bottom : bounds.Bottom
                    )
                    : null;

            if (children[i] is SplitContainer nested)
            {
                childBounds.ShouldNotBeNull();
                VerifyTiling(nested, childBounds.Value);
            }
            else
            {
                children[i].AssignedRect.Width.ShouldBeGreaterThan(0);
                children[i].AssignedRect.Height.ShouldBeGreaterThan(0);
            }
        }
    }

    // ---------- removal ----------

    [Fact]
    public void Removing_window_keeps_survivor_weights()
    {
        var tree = Tree(1, 2, 3); // weights 1, 0.5, 0.5
        tree.Remove(tree.Find(2)!);
        tree.Apply(Area);

        tree.Count.ShouldBe(2);
        tree.Find(1)!.AssignedRect.ShouldBe(new Rect(0, 0, 667, H));
        tree.Find(3)!.AssignedRect.ShouldBe(Rect.FromLtrb(667, 0, W, H));
    }

    [Fact]
    public void Removing_from_nested_container_collapses_it()
    {
        // root H[ V[1,2] , 3 ] — remove 2 → V collapses into root.
        var tree = BuildNested();
        tree.Remove(tree.Find(2)!);
        tree.Apply(Area);

        tree.Count.ShouldBe(2);
        tree.Find(1)!.Parent.ShouldBeSameAs(tree.Root);
        tree.Find(1)!.AssignedRect.ShouldBe(new Rect(0, 0, W / 2, H));
        tree.Find(3)!.AssignedRect.ShouldBe(Rect.FromLtrb(W / 2, 0, W, H));
    }

    [Fact]
    public void Removing_last_window_empties_tree()
    {
        var tree = Tree(7);
        tree.Remove(tree.Find(7)!);

        tree.Root.ShouldBeNull();
        tree.Count.ShouldBe(0);
        tree.Focused.ShouldBeNull();
    }

    [Fact]
    public void Focused_falls_to_previous_neighbor_on_remove()
    {
        var tree = Tree(1, 2, 3); // focused = 3
        tree.Remove(tree.Find(3)!);

        tree.Focused.ShouldBeSameAs(tree.Find(2));
    }

    // ---------- directional focus ----------

    [Fact]
    public void Directional_focus_crosses_containers_deterministically()
    {
        var tree = BuildGrid();

        tree.SetFocused(tree.Find(1)); // top-left
        tree.FocusDirection(Direction.Right).ShouldBeTrue();
        tree.Focused.ShouldBeSameAs(tree.Find(2));

        tree.FocusDirection(Direction.Down).ShouldBeTrue();
        tree.Focused.ShouldBeSameAs(tree.Find(4)); // bottom-right

        tree.FocusDirection(Direction.Up).ShouldBeTrue();
        tree.Focused.ShouldBeSameAs(tree.Find(2));

        tree.FocusDirection(Direction.Left).ShouldBeTrue();
        tree.Focused.ShouldBeSameAs(tree.Find(1));
    }

    [Fact]
    public void Directional_focus_at_edge_is_noop()
    {
        var tree = BuildGrid();
        tree.SetFocused(tree.Find(1));

        tree.FocusDirection(Direction.Left).ShouldBeFalse();
        tree.FocusDirection(Direction.Up).ShouldBeFalse();
        tree.Focused.ShouldBeSameAs(tree.Find(1));
    }

    // ---------- move ----------

    [Fact]
    public void Move_swaps_window_positions_across_containers()
    {
        var tree = BuildGrid();
        tree.SetFocused(tree.Find(1)); // top-left
        var oldRightTop = tree.Find(2)!.AssignedRect;

        tree.MoveFocused(Direction.Right).ShouldBeTrue();
        tree.Apply(Area);

        tree.Find(1)!.AssignedRect.ShouldBe(oldRightTop);
        tree.Focused.ShouldBeSameAs(tree.Find(1), "focus stays with the moved window");
    }

    // ---------- resize ----------

    [Fact]
    public void Resize_uses_nearest_matching_ancestor_axis()
    {
        var tree = BuildGrid();

        // Grow bottom-left window upward: steals height from window 1.
        tree.SetFocused(tree.Find(3));
        tree.ResizeFocused(Direction.Up, 60).ShouldBeTrue();
        tree.Apply(Area);

        tree.Find(1)!.AssignedRect.Height.ShouldBe(240);
        tree.Find(3)!.AssignedRect.Top.ShouldBe(240);
        tree.Find(3)!.AssignedRect.Height.ShouldBe(360);
        // Column widths untouched.
        tree.Find(1)!.AssignedRect.Width.ShouldBe(W / 2);
        tree.Find(2)!.AssignedRect.Width.ShouldBe(W / 2);
    }

    [Fact]
    public void Resize_toward_edge_with_no_border_is_noop()
    {
        var tree = BuildGrid();
        tree.SetFocused(tree.Find(1)); // already at the top of its column

        tree.ResizeFocused(Direction.Up, 60).ShouldBeFalse();
    }

    [Fact]
    public void Resize_clamps_sibling_to_minimum_share()
    {
        var tree = Tree(1, 2);
        tree.SetFocused(tree.Find(1));

        tree.ResizeFocused(Direction.Right, 10_000).ShouldBeTrue();
        tree.Apply(Area);

        // Sibling keeps MinWeightShare (5% of total weight 2 → 50px).
        tree.Find(2)!.AssignedRect.Width.ShouldBe(50);
    }

    [Fact]
    public void Resize_without_matching_ancestor_is_noop()
    {
        var tree = Tree(1, 2);
        tree.SetFocused(tree.Find(1));

        // Root is horizontal-only; resizing Up has no vertical ancestor.
        tree.ResizeFocused(Direction.Up, 60).ShouldBeFalse();
    }

    // ---------- orientation ----------

    [Fact]
    public void Toggle_flips_parent_orientation()
    {
        var tree = Tree(1, 2);

        tree.ToggleOrientation().ShouldBeTrue();
        tree.Apply(Area);

        tree.Find(1)!.AssignedRect.ShouldBe(new Rect(0, 0, W, H / 2));
        tree.Find(2)!.AssignedRect.ShouldBe(Rect.FromLtrb(0, H / 2, W, H));
    }

    [Fact]
    public void Toggle_on_sole_root_leaf_is_noop()
    {
        var tree = Tree(1);
        tree.ToggleOrientation().ShouldBeFalse();
    }

    // ---------- builders ----------

    /// <summary>
    /// Classic 2x2 grid:
    ///   root H[ colLeft V[1,3] | colRight V[2,4] ]
    ///     1 | 2
    ///     ---
    ///     3 | 4
    /// </summary>
    private static SplitTree BuildGrid()
    {
        WindowLeaf Leaf(nint h) => new() { Hwnd = h };

        var colLeft = new SplitContainer { Horizontal = false };
        var colRight = new SplitContainer { Horizontal = false };
        var root = new SplitContainer { Horizontal = true };

        root.Add(colLeft);
        root.Add(colRight);
        colLeft.Add(Leaf(1));
        colLeft.Add(Leaf(3));
        colRight.Add(Leaf(2));
        colRight.Add(Leaf(4));

        var tree = SplitTree.Adopt(root, focused: null);
        tree.Apply(Area);
        return tree;
    }

    /// <summary>root H[ V[1,2] , 3 ]</summary>
    private static SplitTree BuildNested()
    {
        var nested = new SplitContainer { Horizontal = false };
        var leaf1 = new WindowLeaf { Hwnd = 1 };
        var leaf2 = new WindowLeaf { Hwnd = 2 };
        nested.Add(leaf1);
        nested.Add(leaf2);

        var root = new SplitContainer { Horizontal = true };
        root.Add(nested);
        root.Add(new WindowLeaf { Hwnd = 3 });

        var tree = SplitTree.Adopt(root, focused: leaf1);
        tree.Apply(Area);
        return tree;
    }
}
