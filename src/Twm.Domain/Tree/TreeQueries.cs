namespace Twm.Domain.Tree;

/// <summary>Read-only queries over the container tree.</summary>
public static class TreeQueries
{
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
