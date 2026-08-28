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
}
