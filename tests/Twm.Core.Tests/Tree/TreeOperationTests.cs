using Twm.Core.Geometry;
using Twm.Core.Tree;

namespace Twm.Core.Tests.Tree;

public class TreeOperationsTests
{
    [Fact]
    public void AppendChild_SetsParentAndBothLists()
    {
        var split = new SplitContainer(LayoutMode.SplitHorizontal);
        var window = new TilingWindow(new WindowId(1));

        split.AppendChild(window);

        window.Parent.ShouldBe(split);
        split.Children.ShouldBe([window]);
        split.ChildFocusOrder.ShouldBe([window]);
    }

    [Fact]
    public void InsertChild_PlacesInLayoutOrderButAppendsFocusOrder()
    {
        var split = new SplitContainer(LayoutMode.SplitHorizontal);
        var first = new TilingWindow(new WindowId(1));
        var second = new TilingWindow(new WindowId(2));

        split.AppendChild(first);
        split.InsertChild(0, second); // second goes to front of layout order

        split.Children.ShouldBe([second, first]);
        /// Focus order is append-on-insert, so the first-added stays most-recent
        split.ChildFocusOrder.ShouldBe([first, second]);
        first.Index.ShouldBe(1);
        second.Index.ShouldBe(0);
    }

    [Fact]
    public void AppendChild_RejectsAlreadyAttached()
    {
        var a = new SplitContainer();
        var b = new SplitContainer();
        var window = new TilingWindow(new WindowId(1));
        a.AppendChild(window);

        Should.Throw<InvalidOperationException>(() => b.AppendChild(window));
    }

    [Fact]
    public void InsertChild_RejectsIndexOutOfRange()
    {
        var split = new SplitContainer();
        var window = new TilingWindow(new WindowId(1));

        Should.Throw<ArgumentOutOfRangeException>(() => split.InsertChild(1, window));
    }

    [Fact]
    public void RemoveChild_DetachesAndAllowsReattach()
    {
        var split = new SplitContainer();
        var window = new TilingWindow(new WindowId(1));
        split.AppendChild(window);

        split.RemoveChild(window);

        window.Parent.ShouldBeNull();
        split.Children.ShouldBeEmpty();
        split.ChildFocusOrder.ShouldBeEmpty();

        // Detached window can be attached elsewhere
        var other = new SplitContainer();
        other.AppendChild(window);

        window.Parent.ShouldBe(other);
    }

    [Fact]
    public void RemoveChild_RejectsNonChild()
    {
        var split = new SplitContainer();
        var window = new TilingWindow(new WindowId(1));

        Should.Throw<InvalidOperationException>(() => split.RemoveChild(window));
    }

    [Fact]
    public void Siblings_AndNeighbors_ReflectLayoutOrder()
    {
        (_, _, _, SplitContainer split, TilingWindow w10, TilingWindow w20, TilingWindow w30) =
            BuildSampleTree();

        w10.Siblings.ShouldBe([w20]);
        w10.NextSibling.ShouldBe(w20);
        w10.PreviousSibling.ShouldBeNull();
        w20.PreviousSibling.ShouldBe(w10);
        w20.NextSibling.ShouldBeNull();
        split.Siblings.ShouldBe([w30]);
    }

    [Fact]
    public void Ancestors_WalkUpToRoot()
    {
        (
            RootContainer root,
            Monitor monitor,
            Workspace workspace,
            SplitContainer split,
            _,
            TilingWindow w20,
            _
        ) = BuildSampleTree();

        w20.Ancestors.ShouldBe([split, workspace, monitor, root]);
    }

    [Fact]
    public void Descendants_AreBreadthFirst()
    {
        (
            RootContainer root,
            Monitor monitor,
            Workspace workspace,
            SplitContainer split,
            TilingWindow w10,
            TilingWindow w20,
            TilingWindow w30
        ) = BuildSampleTree();

        root.Descendants.ShouldBe([monitor, workspace, split, w30, w10, w20]);
    }

    [Fact]
    public void FocusOrderDefaultsToInsertionOrder()
    {
        (RootContainer root, _, _, SplitContainer split, TilingWindow w10, _, _) =
            BuildSampleTree();

        split.LastFocusedChild.ShouldBe(w10);
        root.LastFocusedDescendant.ShouldBe(w10);
    }

    [Fact]
    public void Focus_BubblesMostRecentUpTheAncestry()
    {
        (RootContainer root, _, Workspace workspace, SplitContainer split, _, TilingWindow w20, _) =
            BuildSampleTree();

        w20.Focus();

        root.LastFocusedDescendant.ShouldBe(w20);
        workspace.LastFocusedChild.ShouldBe(split);
        split.LastFocusedChild.ShouldBe(w20);
        w20.FocusIndex.ShouldBe(0);
    }

    [Fact]
    public void Render_ProducesDeterministicSnapshot()
    {
        (RootContainer root, _, _, _, _, _, _) = BuildSampleTree();

        string expected =
            "Root\n"
            + "  Monitor [0,0 1920x1080]\n"
            + "    Workspace \"1\" Horizontal [0,0 0x0]\n"
            + "      Split Vertical [0,0 0x0]\n"
            + "        Window #10 [0,0 0x0]\n"
            + "        Window #20 [0,0 0x0]\n"
            + "      Window #30 [0,0 0x0]\n";

        TreeRenderer.Render(root).ShouldBe(expected);
    }

    /// <summary>
    /// Builds:
    /// Root -> Monitor -> Workspace "1" (Horizontal)
    ///      -> Split (Vertical) -> [Window #10, Window #20]
    ///      -> Window #30
    /// </summary>
    private static (
        RootContainer Root,
        Monitor Monitor,
        Workspace Workspace,
        SplitContainer Split,
        TilingWindow W10,
        TilingWindow W20,
        TilingWindow W30
    ) BuildSampleTree()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 1920, 1080));
        root.AppendChild(monitor);

        var workspace = new Workspace("1", LayoutMode.SplitHorizontal);
        monitor.AppendChild(workspace);

        var split = new SplitContainer(LayoutMode.SplitVertical);
        workspace.AppendChild(split);

        var w10 = new TilingWindow(new WindowId(10));
        var w20 = new TilingWindow(new WindowId(20));
        split.AppendChild(w10);
        split.AppendChild(w20);

        var w30 = new TilingWindow(new WindowId(30));
        workspace.AppendChild(w30);

        return (root, monitor, workspace, split, w10, w20, w30);
    }
}
