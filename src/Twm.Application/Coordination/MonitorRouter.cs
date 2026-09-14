using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Application.Coordination;

/// <summary>
/// Chooses which <see cref="Monitor" /> a window belongs to by testing which
/// monitor's bounds contain the window's center point. A window whose center
/// lands on no monitor (fully-off screen, or in a taskbar strip that the work
/// area excludes) falls back to the primary monitor, the first child, since
/// <see cref="DesktopBuilder" /> orders it first.
/// </summary>
public static class MonitorRouter
{
    /// <summary>
    /// The monitor that should own a window with the given bounds.
    /// </summary>
    public static Monitor Pick(RootContainer root, Rect windowBounds)
    {
        ArgumentNullException.ThrowIfNull(root);

        Point center = windowBounds.Center;
        Monitor? first = null;

        foreach (Monitor monitor in root.Children.OfType<Monitor>())
        {
            first ??= monitor;
            if (monitor.Bounds.Contains(center))
            {
                return monitor;
            }
        }

        return first
            ?? throw new InvalidOperationException("Root has no monitors to route the window to.");
    }
}
