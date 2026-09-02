using Twm.Application.OutboundPorts;

namespace Twm.TestSupport.Fakes;

/// <summary>
/// An <see cref="IMonitorSystem" /> that returns a fixed set of monitor.
/// </summary>
public sealed class FakeMonitorSystem(params MonitorInfo[] monitors) : IMonitorSystem
{
    private readonly IReadOnlyList<MonitorInfo> _monitors = monitors;

    public IReadOnlyList<MonitorInfo> EnumerateMonitors() => _monitors;
}
