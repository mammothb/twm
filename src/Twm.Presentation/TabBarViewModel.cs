using Twm.Domain.Tree;

namespace Twm.Presentation;

public static class TabBarViewModel
{
    public static IReadOnlyList<TabBarView> Build(
        RootContainer root,
        Func<WindowId, string> titleOf
    )
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(titleOf);

        List<TabBarView> views = [];
        foreach (Container child in root.Children)
        {
            if (child is Monitor monitor && monitor.LastFocusedChild is Container activeWorkspace)
            {
                Collect(activeWorkspace, titleOf, views);
            }
        }

        return views;
    }

    private static void Collect(
        Container container,
        Func<WindowId, string> titleOf,
        List<TabBarView> views
    )
    {
        if (container is not SplitContainer split || split.Children.Count == 0)
        {
            return;
        }

        if (split.Layout is Layout.Stacked or Layout.Tabbed)
        {
            var tabs = new List<TabItem>(split.Children.Count);
            foreach (Container child in split.Children)
            {
                bool isFocused = ReferenceEquals(child, split.LastFocusedChild);
                tabs.Add(new TabItem(RepresentativeTitle(child, titleOf), isFocused));
            }

            views.Add(new TabBarView(split.Id, split.Bounds, split.Layout, tabs));

            if (split.LastFocusedChild is Container focused)
            {
                Collect(focused, titleOf, views);
            }

            return;
        }

        foreach (Container child in split.Children)
        {
            Collect(child, titleOf, views);
        }
    }

    private static string RepresentativeTitle(Container child, Func<WindowId, string> titleOf)
    {
        if (child is TilingWindow window)
        {
            return titleOf(window.WindowId);
        }
        if (child.LastFocusedDescendant is TilingWindow descendant)
        {
            return titleOf(descendant.WindowId);
        }
        return child is SplitContainer split ? $"[{split.Layout}]" : "";
    }
}
