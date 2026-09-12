namespace Twm.Domain.Tree;

/// <summary>
/// The single root of the container tree. Its children are monitors.
/// </summary>
public sealed class RootContainer : Container
{
    /// <summary>The globally focused window, or null.</summary>
    public TilingWindow? FocusedWindow => LastFocusedDescendant as TilingWindow;

    /// <summary>
    /// Finds a managed window by id anywhere under the container.
    /// </summary>
    public TilingWindow? FindWindow(WindowId id)
    {
        foreach (Container descendant in Descendants)
        {
            if (descendant is TilingWindow window && window.WindowId == id)
            {
                return window;
            }
        }

        return null;
    }
}
