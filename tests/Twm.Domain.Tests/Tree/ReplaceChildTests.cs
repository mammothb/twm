using Twm.Domain.Tree;

namespace Twm.Domain.Tests.Tree;

public class ReplaceChildTests
{
    [Fact]
    public void ReplaceChild_WhenOldChildIsReplacedWithNew_SwapsInPlaceAndReparents()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        var w3 = new TilingWindow(new WindowId(3));
        split.AppendChild(w1);
        split.AppendChild(w2);
        split.AppendChild(w3);
        var x = new TilingWindow(new WindowId(99));

        split.ReplaceChild(w2, x);

        split.Children.ShouldBe([w1, x, w3]);
        x.Parent.ShouldBeSameAs(split);
        w2.Parent.ShouldBeNull();
    }

    [Fact]
    public void ReplaceChild_WhenOldChildWasFocused_PreservesFocusStanding()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        split.AppendChild(w1);
        split.AppendChild(w2);
        w2.Focus();

        split.LastFocusedChild.ShouldBeSameAs(w2);
        var x = new TilingWindow(new WindowId(99));

        split.ReplaceChild(w2, x);

        split.LastFocusedChild.ShouldBeSameAs(x);
    }

    [Fact]
    public void ReplaceChild_WhenOldAndNewAreSame_IsNoOp()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        split.AppendChild(w1);

        split.ReplaceChild(w1, w1);

        split.Children.ShouldBe([w1]);
        w1.Parent.ShouldBeSameAs(split);
    }

    [Fact]
    public void ReplaceChild_WhenOldChildHasSizeFraction_DoesNotTransferSize()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1)) { SizeFraction = 3 };
        split.AppendChild(w1);
        var w2 = new TilingWindow(new WindowId(2)) { SizeFraction = 1 };

        split.ReplaceChild(w1, w2);

        w2.SizeFraction.ShouldBe(1);
    }

    [Fact]
    public void ReplaceChild_WhenOldChildIsNotAChild_ThrowsInvalidOperationException()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));

        Should.Throw<InvalidOperationException>(() => split.ReplaceChild(w1, w2));
    }

    [Fact]
    public void ReplaceChild_WhenNewChildIsAlreadyAttached_ThrowsInvalidOperationException()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        split.AppendChild(w1);
        var split2 = new SplitContainer();
        var w2 = new TilingWindow(new WindowId(2));
        split2.AppendChild(w2);

        Should.Throw<InvalidOperationException>(() => split.ReplaceChild(w1, w2));
    }

    [Fact]
    public void ReplaceChild_WhenNewChildWouldCreateCycle_ThrowsInvalidOperationException()
    {
        var root = new SplitContainer();
        var mid = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        root.AppendChild(mid);
        mid.AppendChild(w1);

        Should.Throw<InvalidOperationException>(() => mid.ReplaceChild(w1, root));
    }
}
