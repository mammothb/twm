namespace Twm.Domain.Tree;

/// <summary>
/// The root split of a monitor's layout tree. Identified by
/// <see cref="Name" />, e.g., "1".
/// </summary>
public sealed class Workspace : SplitContainer
{
    public Workspace(string name, Layout layout = Layout.SplitHorizontal)
        : base(layout)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    /// <summary>Display name of number, e.g., "1".</summary>
    public string Name { get; }

    /// <summary>
    /// Adopts a new window into the workspace: next to the workspace's focused
    /// window or filling the workspace when empty. Returns the new window (not
    /// yet focused).
    /// </summary>
    public TilingWindow Adopt(WindowId windowId, WindowId? owner = null)
    {
        var window = new TilingWindow(windowId, owner);

        // Open next to the workspace's focused window, i3-style; otherwise fill
        // the workspace
        if (
            LastFocusedDescendant is TilingWindow focused
            && focused.Parent is SplitContainer parent
        )
        {
            parent.InsertChild(focused.Index + 1, window);
        }
        else
        {
            AppendChild(window);
        }

        return window;
    }
}
