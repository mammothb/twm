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
        Func<WindowId, string> titleGetter,
        DateTimeOffset now
    )
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(titleGetter);

        List<MonitorBarView> views = [];
        int index = 0;
        foreach (Monitor monitor in root.Children.OfType<Monitor>())
        {
            var activeWorkspace = monitor.LastFocusedChild as Workspace;

            List<WorkspaceItem> workspaces = [];
            foreach (Workspace workspace in monitor.Children.OfType<Workspace>())
            {
                bool isActive = ReferenceEquals(workspace, activeWorkspace);
                bool isOccupied = workspace.Descendants.OfType<TilingWindow>().Any();
                workspaces.Add(new WorkspaceItem(workspace.Name, isActive, isOccupied));
            }

            string? focusedTitle = activeWorkspace?.LastFocusedDescendant is TilingWindow focused
                ? titleGetter(focused.WindowId)
                : null;

            views.Add(new MonitorBarView(index, workspaces, focusedTitle));
            index++;
        }

        return new BarSnapshot(views, Clock(now));
    }
}
