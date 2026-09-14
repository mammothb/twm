using Twm.Domain.Tree;

namespace Twm.Presentation;

public static class TabBarViewModel
{
    public static IReadOnlyList<TabBarView> Build(
        RootContainer root,
        Func<WindowId, string> titleGetter
    )
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(titleGetter);

        List<TabBarView> views = [];
        foreach (Monitor monitor in root.Children.OfType<Monitor>())
        {
            if (monitor.LastFocusedChild is Container activeWorkspace)
            {
                Collect(activeWorkspace, titleGetter, views);
            }
        }

        return views;
    }

    private static void Collect(
        Container container,
        Func<WindowId, string> titleGetter,
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
                tabs.Add(new TabItem(RepresentativeTitle(child, titleGetter), isFocused));
            }

            views.Add(new TabBarView(split.Id, split.Bounds, split.Layout, tabs));

            if (split.LastFocusedChild is Container focused)
            {
                Collect(focused, titleGetter, views);
            }

            return;
        }

        foreach (Container child in split.Children)
        {
            Collect(child, titleGetter, views);
        }
    }

    private static string RepresentativeTitle(Container child, Func<WindowId, string> titleGetter)
    {
        if (child is TilingWindow window)
        {
            return titleGetter(window.WindowId);
        }

        if (child.LastFocusedDescendant is TilingWindow descendant)
        {
            return titleGetter(descendant.WindowId);
        }

        return child is SplitContainer split ? $"[{split.Layout}]" : "";
    }
}
