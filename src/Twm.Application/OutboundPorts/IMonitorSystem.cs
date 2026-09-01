namespace Twm.Application.OutboundPorts;

/// <summary>
/// The OS display topology. Implemented by the Win32 layer and faked in tests,
/// so the tree-bootstrap logic is verifiable in Linux.
/// </summary>
public interface IMonitorSystem
{
    /// <summary>
    /// Enumerates the currently connected monitors. The order is unspecified;
    /// calleds identify the primary via <see cref="MonitorInfo.IsPrimary" />.
    /// </summary>
    IReadOnlyList<MonitorInfo> EnumerateMonitors();
}
