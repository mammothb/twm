namespace Twm.Domain.Tree;

/// <summary>
/// The single root of the container tree. Its children are monitors.
/// </summary>
public sealed class RootContainer : Container
{
    /// <summary>The globally focused window, or null.</summary>
    public TilingWindow? FocusedWindow => LastFocusedDescendant as TilingWindow;
}
