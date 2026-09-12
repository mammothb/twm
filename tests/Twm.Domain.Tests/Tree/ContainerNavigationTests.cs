using Twm.Domain.Tree;

namespace Twm.Domain.Tests.Tree;

public class ContainerNavigationTests
{
    [Fact]
    public void Index_WhenContainerIsDetached_ReturnsZero()
    {
        var detached = new TilingWindow(new WindowId(1));

        detached.Index.ShouldBe(0);
    }

    [Fact]
    public void Index_WhenContainerIsFirstChild_ReturnsZero()
    {
        (_, TilingWindow w1, TilingWindow w2, _) = BuildSplit();

        w1.Index.ShouldBe(0);
        w2.Index.ShouldBe(1);
    }

    [Fact]
    public void Index_WhenContainerIsMiddleChild_ReturnsPosition()
    {
        (_, _, TilingWindow w2, _) = BuildSplit();

        w2.Index.ShouldBe(1);
    }

    [Fact]
    public void Index_WhenContainerIsLastChild_ReturnsPosition()
    {
        (_, _, _, TilingWindow w3) = BuildSplit();

        w3.Index.ShouldBe(2);
    }

    [Fact]
    public void FocusIndex_WhenContainerIsDetached_ReturnsZero()
    {
        var detached = new TilingWindow(new WindowId(1));

        detached.FocusIndex.ShouldBe(0);
    }

    [Fact]
    public void FocusIndex_WhenContainerIsMostRecentlyFocused_ReturnsZero()
    {
        (_, TilingWindow w1, TilingWindow w2, _) = BuildSplit();
        w1.Focus();

        w1.FocusIndex.ShouldBe(0);
    }

    [Fact]
    public void FocusIndex_WhenContainerIsNotMostRecentlyFocused_ReturnsPosition()
    {
        (_, TilingWindow w1, TilingWindow w2, _) = BuildSplit();
        w1.Focus();
        w2.Focus();

        w1.FocusIndex.ShouldBe(1);
    }

    [Fact]
    public void NextSibling_WhenContainerIsDetached_ReturnsNull()
    {
        var detached = new TilingWindow(new WindowId(1));

        detached.NextSibling.ShouldBeNull();
    }

    [Fact]
    public void NextSibling_WhenContainerIsOnlyChild_ReturnsNull()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        split.AppendChild(w1);

        w1.NextSibling.ShouldBeNull();
    }

    [Fact]
    public void NextSibling_WhenContainerIsFirstOfMany_ReturnsSecond()
    {
        (_, TilingWindow w1, TilingWindow w2, _) = BuildSplit();

        w1.NextSibling.ShouldBeSameAs(w2);
    }

    [Fact]
    public void NextSibling_WhenContainerIsLastChild_ReturnsNull()
    {
        (_, _, _, TilingWindow w3) = BuildSplit();

        w3.NextSibling.ShouldBeNull();
    }

    [Fact]
    public void PreviousSibling_WhenContainerIsDetached_ReturnsNull()
    {
        var detached = new TilingWindow(new WindowId(1));

        detached.PreviousSibling.ShouldBeNull();
    }

    [Fact]
    public void PreviousSibling_WhenContainerIsFirstChild_ReturnsNull()
    {
        (_, TilingWindow w1, TilingWindow w2, _) = BuildSplit();

        w1.PreviousSibling.ShouldBeNull();
    }

    [Fact]
    public void PreviousSibling_WhenContainerIsMiddleChild_ReturnsPrior()
    {
        (_, TilingWindow w1, TilingWindow w2, _) = BuildSplit();

        w2.PreviousSibling.ShouldBeSameAs(w1);
    }

    [Fact]
    public void PreviousSibling_WhenContainerIsLastChild_ReturnsPrior()
    {
        (_, _, TilingWindow w2, TilingWindow w3) = BuildSplit();

        w3.PreviousSibling.ShouldBeSameAs(w2);
    }

    [Fact]
    public void Siblings_WhenContainerIsDetached_ReturnsEmpty()
    {
        var detached = new TilingWindow(new WindowId(1));

        detached.Siblings.ShouldBeEmpty();
    }

    [Fact]
    public void Siblings_WhenContainerIsOnlyChild_ReturnsEmpty()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        split.AppendChild(w1);

        w1.Siblings.ShouldBeEmpty();
    }

    [Fact]
    public void Siblings_WhenContainerHasMultipleSiblings_ExcludesSelf()
    {
        (_, TilingWindow w1, TilingWindow w2, TilingWindow w3) = BuildSplit();

        w2.Siblings.ShouldBe([w1, w3]);
        w2.Siblings.ShouldNotContain(w2);
    }

    [Fact]
    public void Siblings_WhenContainerHasMultipleSiblings_ReturnsAllOthers()
    {
        (_, TilingWindow w1, TilingWindow w2, TilingWindow w3) = BuildSplit();

        w1.Siblings.ShouldBe([w2, w3]);
        w3.Siblings.ShouldBe([w1, w2]);
    }

    /// <summary>
    /// Builds a SplitContainer with three child TilingWindows appended in
    /// order. The detached-container and only-child tests don't use the
    /// helper because their setups are intentionally minimal (no parent at
    /// all, or a parent with one child).
    /// </summary>
    private static (
        SplitContainer Split,
        TilingWindow W1,
        TilingWindow W2,
        TilingWindow W3
    ) BuildSplit()
    {
        var split = new SplitContainer();
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        var w3 = new TilingWindow(new WindowId(3));
        split.AppendChild(w1);
        split.AppendChild(w2);
        split.AppendChild(w3);
        return (split, w1, w2, w3);
    }
}
