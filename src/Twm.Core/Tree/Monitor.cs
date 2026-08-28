using Twm.Core.Geometry;

namespace Twm.Core.Tree;

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
}
