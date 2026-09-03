using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;
using Twm.Presentation;

namespace TWm.Presentation.Tests;

public class TabBarViewModelTests
{
    private static string Title(WindowId id) => $"win{id.Value}";

    private static (RootContainer Root, Monitor Monitor, Workspace Workspace) Desktop(Layout layout)
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws = new Workspace("1", layout);
        monitor.AppendChild(ws);
        return (root, monitor, ws);
    }

    [Fact]
    public void TabbedWorkspace_EmitsOneBarWithTabsAndFocusedFlag()
    {
        (RootContainer root, _, Workspace ws) = Desktop(Layout.Tabbed);
        ws.AppendChild(new TilingWindow(new WindowId(1)));
        var w2 = new TilingWindow(new WindowId(2));
        ws.AppendChild(w2);
        ws.AppendChild(new TilingWindow(new WindowId(3)));
        w2.Focus();
        new LayoutEngine().Arrange(root);

        TabBarView view = TabBarViewModel.Build(root, Title).ShouldHaveSingleItem();

        view.Layout.ShouldBe(Layout.Tabbed);
        view.Bounds.ShouldBe(new Rect(0, 0, 800, 600));
        view.Tabs.Select(t => t.Title).ShouldBe(["win1", "win2", "win3"]);
        view.Tabs.Select(t => t.Focused).ShouldBe([false, true, false]);
    }

    [Fact]
    public void DescendsOnlyIntoTheFocusedTab()
    {
        (RootContainer root, _, Workspace ws) = Desktop(Layout.Tabbed);
        var focusedTab = new SplitContainer(Layout.Tabbed);
        var otherTab = new SplitContainer(Layout.Tabbed);
        ws.AppendChild(focusedTab);
        ws.AppendChild(otherTab);
        var inner = new TilingWindow(new WindowId(1));
        focusedTab.AppendChild(inner);
        otherTab.AppendChild(new TilingWindow(new WindowId(2)));
        inner.Focus();
        new LayoutEngine().Arrange(root);

        IReadOnlyList<TabBarView> views = TabBarViewModel.Build(root, Title);

        views.Count.ShouldBe(2);
        views.ShouldContain(v => v.ContainerId == ws.Id);
        views.ShouldContain(v => v.ContainerId == focusedTab.Id);
        views.ShouldNotContain(v => v.ContainerId == otherTab.Id);
    }

    [Fact]
    public void SplitWorkspace_EmitsNoOwnBar_ButDescendsIntoNestedTabbed()
    {
        (RootContainer root, _, Workspace ws) = Desktop(Layout.SplitHorizontal);
        ws.AppendChild(new TilingWindow(new WindowId(1)));
        var nested = new SplitContainer(Layout.Tabbed);
        ws.AppendChild(nested);
        var nestedWindow = new TilingWindow(new WindowId(2));
        nested.AppendChild(nestedWindow);
        nested.AppendChild(new TilingWindow(new WindowId(3)));
        nestedWindow.Focus();
        new LayoutEngine().Arrange(root);

        TabBarView view = TabBarViewModel.Build(root, Title).ShouldHaveSingleItem();

        view.ContainerId.ShouldBe(nested.Id);
        view.Layout.ShouldBe(Layout.Tabbed);
    }

    [Fact]
    public void InactiveWorkspace_TabbedContainer_ProducesNoBar()
    {
        (RootContainer root, Monitor monitor, Workspace active) = Desktop(Layout.SplitHorizontal);
        var inactive = new Workspace("2", Layout.Tabbed);
        monitor.AppendChild(inactive);
        inactive.AppendChild(new TilingWindow(new WindowId(2)));
        var activeWindow = new TilingWindow(new WindowId(1));
        active.AppendChild(activeWindow);
        activeWindow.Focus();
        new LayoutEngine().Arrange(root);

        TabBarViewModel.Build(root, Title).ShouldBeEmpty();
    }
}
