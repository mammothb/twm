using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;

namespace Twm.Adapters.Windows;

/// <summary>
/// The Win32 <see cref="IMonitorSystem" />: enumerates displays via
/// <c>EnumDisplayMonitors</c> + <c>GetMonitorInfo</c>.
/// </summary>
public sealed class WindowsMonitorSystem : IMonitorSystem
{
    public IReadOnlyList<MonitorInfo> EnumerateMonitors()
    {
        List<MonitorInfo> result = [];
        foreach (nint monitor in NativeMethods.MonitorHandles())
        {
            if (
                NativeMethods.TryGetMonitorInfo(
                    monitor,
                    out Rect bounds,
                    out Rect workArea,
                    out bool isPrimary
                )
            )
            {
                result.Add(new MonitorInfo(new MonitorId(monitor), bounds, workArea, isPrimary));
            }
        }
        return result;
    }
}
