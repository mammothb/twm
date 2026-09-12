using Twm.Domain.Geometry;

namespace Twm.Domain.Tree;

/// <summary>
/// A physical display. Holds workspaces; only one workspace is shown at a time,
/// filling the display bounds.
/// </summary>
public sealed class Monitor : Container
{
    public Monitor(Rect displayBounds)
    {
        Bounds = displayBounds;
    }

    /// <summary>
    /// The monitor's active workspace, its most-recently-focused workspace, or
    /// its first workspace if none has been focused. Null if the monitor has no
    /// workspaces.
    /// </summary>
    public Workspace? ActiveWorkspace =>
        LastFocusedChild as Workspace ?? Children.OfType<Workspace>().FirstOrDefault();
}
