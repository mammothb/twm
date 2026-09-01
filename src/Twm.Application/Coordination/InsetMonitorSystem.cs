using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;

namespace Twm.Application.Coordination;

/// <summary>
/// Which edge the status bar occupies (and thus which edge tiling area loses).
/// </summary>
public enum BarPosition
{
    Top,
    Bottom,
}

/// <summary>
/// An <see cref="IMonitorSystem" /> decorator that shrinks each monitor's
/// <see cref="MonitorInfo.WorkArea" /> by the bar height on the bar's edge, so
/// tiled windows lay out clear of the bar. <see cref="MonitorInfo.Bounds" /> is
/// untouched.
/// </summary>
public sealed class InsetMonitorSystem : IMonitorSystem
{
    private readonly IMonitorSystem _inner;
    private readonly int _barHeight;
    private readonly BarPosition _position;

    public InsetMonitorSystem(IMonitorSystem inner, int barHeight, BarPosition position)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
        _barHeight = barHeight;
        _position = position;
    }

    public IReadOnlyList<MonitorInfo> EnumerateMonitors()
    {
        List<MonitorInfo> result = [];
        foreach (MonitorInfo monitor in _inner.EnumerateMonitors())
        {
            result.Add(monitor with { WorkArea = Inset(monitor.WorkArea) });
        }
        return result;
    }

    private Rect Inset(Rect workArea)
    {
        // never shrink past the monitor
        int height = Math.Min(_barHeight, workArea.Height);
        return _position == BarPosition.Top
            ? new Rect(workArea.X, workArea.Y + height, workArea.Width, workArea.Height - height)
            : new Rect(workArea.X, workArea.Y, workArea.Width, workArea.Height - height);
    }
}
