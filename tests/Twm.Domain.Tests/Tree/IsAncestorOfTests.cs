using Twm.Domain.Tree;

namespace Twm.Domain.Tests.Tree;

public class IsAncestorOfTests
{
    private static (SplitContainer Root, SplitContainer Mid, TilingWindow Leaf) Chain()
    {
        var root = new SplitContainer();
        var mid = new SplitContainer();
        var leaf = new TilingWindow(new WindowId(1));
        root.AppendChild(mid);
        mid.AppendChild(leaf);
        return (root, mid, leaf);
    }

    [Fact]
    public void TrueForDirectAndIndirectDescendants()
    {
        (SplitContainer root, SplitContainer mid, TilingWindow leaf) = Chain();

        mid.IsAncestorOf(leaf).ShouldBeTrue();
        root.IsAncestorOf(leaf).ShouldBeTrue();
        root.IsAncestorOf(mid).ShouldBeTrue();
    }

    [Fact]
    public void FalseForDescendantsAskedAboutItsAncestor()
    {
        (SplitContainer root, SplitContainer mid, TilingWindow leaf) = Chain();

        leaf.IsAncestorOf(root).ShouldBeFalse();
        leaf.IsAncestorOf(mid).ShouldBeFalse();
    }

    [Fact]
    public void FalseForSelf()
    {
        (SplitContainer root, _, _) = Chain();

        root.IsAncestorOf(root).ShouldBeFalse();
    }

    [Fact]
    public void FalseForNull()
    {
        (SplitContainer root, _, _) = Chain();

        root.IsAncestorOf(null).ShouldBeFalse();
    }

    [Fact]
    public void FalseAcrossDisconnectedTrees()
    {
        (SplitContainer root, _, _) = Chain();
        var w2 = new TilingWindow(new WindowId(2));

        root.IsAncestorOf(w2).ShouldBeFalse();
    }
}
