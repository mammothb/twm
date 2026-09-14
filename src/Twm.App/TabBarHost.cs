using Twm.Adapters.Windows;
using Twm.Application.Config;
using Twm.Application.Coordination;
using Twm.Presentation;

namespace Twm.App;

/// <summary>
/// Owns the <see cref="TabBarManager" />. Subscribes to layout-change
/// events on the WM bus and repaints the tab/stack bars on each. Always
/// constructed (the bar is decorative, not gated by user config).
/// </summary>
internal sealed class TabBarHost : IDisposable
{
    private readonly TabBarManager _tabBarManager;
    private readonly WmSession _session;
    private readonly WindowsWindowSystem _windowSystem;
    private bool _disposed;

    public TabBarHost(WmSession session, WindowsWindowSystem windowSystem, TabOptions options)
    {
        _session = session;
        _windowSystem = windowSystem;
        _tabBarManager = new TabBarManager(
            options.Background,
            options.Foreground,
            options.ActiveBackground,
            options.Height
        );

        _session.Subscribe<LayoutChangedEvent>(_ => Refresh());

        Refresh();
    }

    /// <summary>Rebuilds and repaints the tab/stack bars from the current tree.</summary>
    public void Refresh() =>
        _tabBarManager.Update(TabBarViewModel.Build(_session.Root, _windowSystem.GetTitle));

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _tabBarManager.Dispose();
    }
}
