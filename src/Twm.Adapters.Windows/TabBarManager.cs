using Twm.Presentation;

namespace Twm.Adapters.Windows;

/// <summary>
/// Coordinates one <see cref="TabBarWindow" /> per visible tabbed/stacked
/// container, keyed by the container's id. On each update it creates/reuses a
/// bar for every container in the view model and destroys bars for containers
/// that are no longer tabbed/stacked (or not longer visible).
/// </summary>
public sealed class TabBarManager(uint background, uint foreground, uint accent, int rowHeight)
    : IDisposable
{
    private readonly uint _background = background;
    private readonly uint _foreground = foreground;
    private readonly uint _accent = accent;
    private readonly int _rowHeight = rowHeight;
    private readonly Dictionary<Guid, TabBarWindow> _idToBar = [];

    public void Update(IReadOnlyList<TabBarView> views)
    {
        ArgumentNullException.ThrowIfNull(views);

        HashSet<Guid> liveIds = [];
        foreach (TabBarView view in views)
        {
            liveIds.Add(view.ContainerId);
            if (!_idToBar.TryGetValue(view.ContainerId, out TabBarWindow? bar))
            {
                bar = new TabBarWindow(_background, _foreground, _accent, _rowHeight);
                _idToBar[view.ContainerId] = bar;
            }

            bar.Render(view);
        }

        List<Guid> staleIds = [.. _idToBar.Keys.Where(id => !liveIds.Contains(id))];
        foreach (Guid id in staleIds)
        {
            _idToBar[id].Dispose();
            _idToBar.Remove(id);
        }
    }

    public void Dispose()
    {
        foreach (TabBarWindow bar in _idToBar.Values)
        {
            bar.Dispose();
        }

        _idToBar.Clear();
        TabBarWindow.UnregisterSharedClass();
    }
}
