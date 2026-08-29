namespace Twm.Core.Tree;

/// <summary>A leaf container wrapping a single managed OS window.</summary>
public sealed class TilingWindow(WindowId windowId) : Container
{
    /// <summary>The identity of the wrapped OS window.</summary>
    public WindowId WindowId { get; } = windowId;
}
