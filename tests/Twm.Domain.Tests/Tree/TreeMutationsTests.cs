using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Domain.Tests.Tree;

public class TreeMutationsTests
{
    [Fact]
    public void Cleanup_WhenStartIsNull_IsNoOp()
    {
        Container? nullContainer = null;

        // Should not throw.
        nullContainer.Cleanup();
    }

    [Fact]
    public void Cleanup_WhenStartIsWorkspace_IsNoOp()
    {
        // Workspace is excluded by the `and not Workspace` pattern. Even if
        // a Workspace ends up single-child somehow, Cleanup does not flatten
        // it.
        var ws = new Workspace("1");
        ws.Cleanup();

        ws.Parent.ShouldBeNull();
    }

    [Fact]
    public void Cleanup_WhenSplitHasMultipleChildren_IsNoOp()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        split.AppendChild(w1);
        split.AppendChild(w2);

        split.Cleanup();

        split.Children.ShouldBe([w1, w2]);
    }

    [Fact]
    public void Cleanup_WhenSplitHasNoChildren_RemovesSplitFromParent()
    {
        var parent = new SplitContainer();
        var empty = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        parent.AppendChild(w1);
        parent.AppendChild(empty);
        parent.AppendChild(w2);

        empty.Cleanup();

        parent.Children.ShouldBe([w1, w2]);
    }

    [Fact]
    public void Cleanup_WhenSplitHasOneChild_FlattensIntoParentAndPreservesSizeFraction()
    {
        var parent = new SplitContainer();
        var single = new SplitContainer { SizeFraction = 0.4 };
        var only = new TilingWindow(new WindowId(1)) { SizeFraction = 1.0 };
        var w1 = new TilingWindow(new WindowId(2));
        var w2 = new TilingWindow(new WindowId(3));
        single.AppendChild(only);
        parent.AppendChild(w1);
        parent.AppendChild(single);
        parent.AppendChild(w2);

        single.Cleanup();

        parent.Children.ShouldBe([w1, only, w2]);
        only.SizeFraction.ShouldBe(0.4);
    }

    [Fact]
    public void Cleanup_WhenSplitHasNoParent_IsNoOp()
    {
        // SplitContainer with no parent (e.g., direct child of RootContainer,
        // which is not a SplitContainer). The `split.Parent is Container
        // parent` guard exits the loop after one iteration.
        var split = new SplitContainer();
        var only = new TilingWindow(new WindowId(1));
        split.AppendChild(only);

        split.Cleanup();

        split.Children.ShouldBe([only]);
        only.Parent.ShouldBeSameAs(split);
    }

    [Fact]
    public void Cleanup_WhenChainOfSingleChildSplits_FlattensAllTheWayUp()
    {
        // root (Split) -> single2 -> single1 -> only
        // Calling Cleanup on single1 walks up through single2 and flattens
        // both. Single1 (containing only) becomes just `only` in root; then
        // root has single2 (which now contains only) and itself isn't
        // single-child, so cleanup stops.
        var root = new SplitContainer();
        var single2 = new SplitContainer();
        var single1 = new SplitContainer();
        var only = new TilingWindow(new WindowId(1));
        var w1 = new TilingWindow(new WindowId(2));
        root.AppendChild(single2);
        single2.AppendChild(single1);
        single1.AppendChild(only);
        root.AppendChild(w1);

        single1.Cleanup();

        root.Children.ShouldBe([only, w1]);
    }

    [Fact]
    public void Cleanup_StopsAtWorkspace_DoesNotFlattenWorkspaces()
    {
        // workspace -> split (single child) -> only
        // Cleanup on split flattens it into the workspace (which is now
        // single-child), but the workspace itself is never flattened by
        // Cleanup's `and not Workspace` guard.
        var monitor = new Monitor(new Rect(0, 0, 100, 100));
        var workspace = new Workspace("1");
        var split = new SplitContainer();
        var only = new TilingWindow(new WindowId(1));
        monitor.AppendChild(workspace);
        workspace.AppendChild(split);
        split.AppendChild(only);

        split.Cleanup();

        workspace.Children.ShouldBe([only]);
        // Workspace is still attached to monitor — never removed.
        workspace.Parent.ShouldBeSameAs(monitor);
        // Cleanup walking further up would try to remove monitor's single
        // workspace, but monitor is not a SplitContainer, so the walk stops
        // before reaching the "remove" branch.
        monitor.Children.ShouldBe([workspace]);
    }
}
