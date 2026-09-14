using Twm.Adapters.Windows;
using Twm.Application.Config;
using Twm.Application.Coordination;
using Twm.Application.OutboundPorts;
using Twm.Presentation;

namespace Twm.App;

/// <summary>
/// Owns the per-monitor <see cref="StatusBarManager" /> when
/// <see cref="BarOptions.Enabled" /> is true. Subscribes to layout-change
/// events on the WM bus, runs the 1-second clock tick, and repaints the
/// bar. Constructed only when the option enables it; otherwise
/// <c>Program.cs</c> passes <c>null</c> in place of this host.
/// </summary>
internal sealed class StatusBarHost : IDisposable
{
    private readonly StatusBarManager _statusBarManager;
    private readonly WmSession _session;
    private readonly IMonitorSystem _monitorSystem;
    private readonly WindowsWindowSystem _windowSystem;
    private readonly nuint _clockTimer;
    private string _lastBarClock;
    private bool _disposed;

    public StatusBarHost(
        WmSession session,
        IMonitorSystem monitorSystem,
        WindowsWindowSystem windowSystem,
        BarOptions options
    )
    {
        _session = session;
        _monitorSystem = monitorSystem;
        _windowSystem = windowSystem;
        _statusBarManager = new StatusBarManager(
            DesktopBuilder.OrderPrimaryFirst(_monitorSystem.EnumerateMonitors()),
            options
        );
        _lastBarClock = "";
        _clockTimer = MessageLoop.StartTimer(1000);

        _session.Subscribe<LayoutChangedEvent>(_ => Refresh());
        _session.Subscribe<DisplaysReconciledEvent>(_ =>
        {
            _statusBarManager.SyncMonitors(
                DesktopBuilder.OrderPrimaryFirst(_monitorSystem.EnumerateMonitors())
            );
            Refresh();
        });

        Refresh();
    }

    /// <summary>Rebuilds the bar snapshot from the current tree and repaints.</summary>
    public void Refresh()
    {
        BarSnapshot snapshot = BarViewModel.Build(
            _session.Root,
            _windowSystem.GetTitle,
            DateTimeOffset.Now
        );
        _statusBarManager.Update(snapshot);
        _lastBarClock = snapshot.Clock;
    }

    /// <summary>
    /// One tick from the WM clock timer. Skips a refresh when the displayed
    /// clock string has not changed since the last <see cref="Refresh" />.
    /// </summary>
    public void OnClockTick()
    {
        string now = BarViewModel.Clock(DateTimeOffset.Now);
        if (now == _lastBarClock)
        {
            return;
        }

        _lastBarClock = now;
        Refresh();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        MessageLoop.StopTimer(_clockTimer);
        _statusBarManager.Dispose();
    }
}
