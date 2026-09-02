namespace Twm.Domain.Tree;

/// <summary>A leaf container wrapping a single managed OS window.</summary>
public sealed class TilingWindow(WindowId windowId) : Container
{
    /// <summary>The identity of the wrapped OS window.</summary>
    public WindowId WindowId { get; } = windowId;

    /// <summary>
    /// Whether this window should currently be shown on screen: it is on its
    /// monitor's active workspace <b>and</b>, through every tabbed/stacked
    /// ancestor up to the workspace, its branch is that containers's focused
    /// child (a non-focused tab is hidden). This is the single truth the
    /// reconciler shows/cloaks by, and the hide-event classifier reads.
    public bool IsEffectivelyVisible()
    {
        Workspace? workspace = this.WorkspaceOf();
        Container? activeWorkspace = workspace?.MonitorOf()?.LastFocusedChild;
        if (workspace is null || !ReferenceEquals(workspace, activeWorkspace))
        {
            return false;
        }

        // Walk window -> ... -> workspace (stops when the parent is the
        // Monitor). Any tabbed/stacked split on the path must have the branch
        // we came up through as its focused child.
        Container node = this;
        while (node.Parent is SplitContainer split)
        {
            if (!split.Layout.IsSplit() && !ReferenceEquals(split.LastFocusedChild, node))
            {
                return false;
            }
            node = split;
        }
        return true;
    }
}
