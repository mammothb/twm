using Twm.Application.Config;
using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.Presentation;

namespace Twm.Adapters.Windows;

/// <summary>
/// Owns one <see cref="StatusBarWindow" /> per monitor, positioned along each
/// monitor's top edge, and pushes a <see cref="BarSnapshot" /> to them.
/// Monitors must be supplied in the same order the tree uses (primary-first,
/// then left-to-right, see <see cref="DesktopBuilder.OrderPrimaryFirst" />),
/// so each <see cref="MonitorBarView.Index" /> pairs with the bar on the right
/// display.
/// </summary>
public sealed class StatusBarManager : IDisposable
{
    private readonly List<StatusBarWindow> _bars = [];

    public StatusBarManager(IReadOnlyList<MonitorInfo> orderedMonitors, BarOptions options)
    {
        ArgumentNullException.ThrowIfNull(orderedMonitors);
        ArgumentNullException.ThrowIfNull(options);
        foreach (MonitorInfo monitor in orderedMonitors)
        {
            // Sit at the work-area edge (respects a taskbar); the
            // InsetMonitorSystem removes this same strip from the tiling area
            // so windows don't overlap the bar
            Rect area = monitor.WorkArea;
            int y =
                options.Position == BarPosition.Top
                    ? area.Y
                    : area.Y + area.Height - options.Height;
            _bars.Add(
                new StatusBarWindow(new Rect(area.X, y, area.Width, options.Height), options)
            );
        }
    }

    /// <summary>
    /// Repaints every bar from the snapshot (pairing by
    /// <see cref="MonitorBarView.Index" />).
    /// </summary>
    public void Update(BarSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        foreach (MonitorBarView view in snapshot.Monitors)
        {
            if (0 <= view.Index && view.Index < _bars.Count)
            {
                _bars[view.Index].Render(view, snapshot.Clock);
            }
        }
    }

    public void Dispose()
    {
        foreach (StatusBarWindow bar in _bars)
        {
            bar.Dispose();
        }
        _bars.Clear();
        StatusBarWindow.UnregisterSharedClass();
    }
}
