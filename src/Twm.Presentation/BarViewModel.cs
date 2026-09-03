using System.Globalization;
using Twm.Domain.Tree;

namespace Twm.Presentation;

/// <summary>
/// Projects the container tree into a <see cref="BarSnapshot" />, exactly what
/// each monitor's bar draws.
/// </summary>
public static class BarViewModel
{
    private const string ClockFormat = "HH:mm";

    /// <summary>The bar clock string for a moment.</summary>
    public static string Clock(DateTimeOffset now) =>
        now.ToString(ClockFormat, CultureInfo.InvariantCulture);

    public static BarSnapshot Build(
        RootContainer root,
        Func<WindowId, string> titleOf,
        DateTimeOffset now
    )
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(titleOf);

        List<MonitorBarView> views = [];
        int index = 0;
        foreach (Monitor monitor in root.Children.OfType<Monitor>())
        {
            var active = monitor.LastFocusedChild as Workspace;

            List<WorkspaceItem> items = [];
            foreach (Workspace workspace in monitor.Children.OfType<Workspace>())
            {
                bool isActive = ReferenceEquals(workspace, active);
                bool isOccupied = workspace.Descendants.OfType<TilingWindow>().Any();
                items.Add(new WorkspaceItem(workspace.Name, isActive, isOccupied));
            }

            string? focusedTitle = active?.LastFocusedDescendant is TilingWindow focused
                ? titleOf(focused.WindowId)
                : null;

            views.Add(new MonitorBarView(index, items, focusedTitle));
            index++;
        }

        return new BarSnapshot(views, Clock(now));
    }
}
