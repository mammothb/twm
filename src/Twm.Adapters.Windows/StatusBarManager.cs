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
    private readonly BarOptions _options;

    public StatusBarManager(IReadOnlyList<MonitorInfo> orderedMonitors, BarOptions options)
    {
        ArgumentNullException.ThrowIfNull(orderedMonitors);
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        foreach (MonitorInfo monitor in orderedMonitors)
        {
            _bars.Add(new StatusBarWindow(BarRect(monitor), options));
        }
    }

    public void SyncMonitors(IReadOnlyList<MonitorInfo> orderedMonitors)
    {
        ArgumentNullException.ThrowIfNull(orderedMonitors);

        // drop bars for monitors that went away from the tail
        while (_bars.Count > orderedMonitors.Count)
        {
            _bars[^1].Dispose();
            _bars.RemoveAt(_bars.Count - 1);
        }

        // add bars for newly attached monitors
        while (_bars.Count < orderedMonitors.Count)
        {
            _bars.Add(new StatusBarWindow(BarRect(orderedMonitors[_bars.Count]), _options));
        }

        // move every surviving bar to its monitor's edge
        for (int i = 0; i < _bars.Count; i++)
        {
            _bars[i].MoveTo(BarRect(orderedMonitors[i]));
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

    private Rect BarRect(MonitorInfo monitor)
    {
        Rect area = monitor.WorkArea;
        int y =
            _options.Position == BarPosition.Top ? area.Y : area.Y + area.Height - _options.Height;
        return new Rect(area.X, y, area.Width, _options.Height);
    }
}
