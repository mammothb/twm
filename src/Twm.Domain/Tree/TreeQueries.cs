namespace Twm.Domain.Tree;

/// <summary>Read-only queries over the container tree.</summary>
public static class TreeQueries
{
    /// <summary>The globally focused window, or null.</summary>
    public static TilingWindow? FocusedWindow(this RootContainer root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return root.LastFocusedDescendant as TilingWindow;
    }

    /// <summary>
    /// Finds a managed window by id anywhere under the container.
    /// </summary>
    public static TilingWindow? FindWindow(this Container container, WindowId id)
    {
        ArgumentNullException.ThrowIfNull(container);
        foreach (Container descendant in container.Descendants)
        {
            if (descendant is TilingWindow window && window.WindowId == id)
            {
                return window;
            }
        }
        return null;
    }

    /// <summary>
    /// Finds a workspace by name anywhere under the container.
    /// </summary>
    public static Workspace? FindWorkspace(this Container container, string name)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(name);
        foreach (Container descendant in container.Descendants)
        {
            if (descendant is Workspace workspace && workspace.Name == name)
            {
                return workspace;
            }
        }
        return null;
    }

    /// <summary>The monitor a container belongs to, or null if detached.</summary>
    public static Monitor? MonitorOf(this Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        foreach (Container ancestor in container.Ancestors)
        {
            if (ancestor is Monitor monitor)
            {
                return monitor;
            }
        }
        return null;
    }

    /// <summary>The workspace a container belongs to, or null if detached.</summary>
    public static Workspace? WorkspaceOf(this Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        foreach (Container ancestor in container.Ancestors)
        {
            if (ancestor is Workspace workspace)
            {
                return workspace;
            }
        }
        return null;
    }
}
