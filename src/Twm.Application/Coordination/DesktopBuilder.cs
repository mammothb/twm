using System.Globalization;
using Twm.Application.Config;
using Twm.Application.OutboundPorts;
using Twm.Domain.Tree;

namespace Twm.Application.Coordination;

/// <summary>
/// Builds the initial container tree from the OS display topology: one
/// <see cref="Monitor" /> per display (sized to its
/// <see cref="MonitorInfo.WorkArea" />, so windows tile clear of the taskbar)
/// with <see cref="WorkspacesPerMonitor" /> workspaces each. Monitors are
/// ordered primary-first, then left-to-right, and workspace names are
/// interleaved round-robin across monitors so names stay globally unique and a
/// single keystroke reaches any workspace on any monitor (2 monitors -> monitor
/// 0 gets 1,3,5,7 and monitor 1 gets 2,4,6,8).
public static class DesktopBuilder
{
    /// <summary>
    /// Default workspaces per monitor when config specifies neither a count nor
    /// names.
    /// </summary>
    public const int WorkspacesPerMonitor = 4;

    /// <summary>
    /// Builds a fresh <see cref="RootContainer" /> from the enumerated monitors
    /// and optional workspace config: a <c>perMonitor</c> count (defaut
    /// <see cref="WorkspacesPerMonitor" />) or an explicit <c>names</c> list.
    /// Names, generated <c>"1".."N"</c> or the explicit list, are distributed
    /// <b>round-robin</b> across monitors
    /// (name[i] -> monitor[i % monitorCount]), which keeps names globally
    /// unique and lets a single keystroke reach any monitor's workspace
    public static RootContainer Build(
        IReadOnlyList<MonitorInfo> monitors,
        WorkspaceOptions? workspaces = null
    )
    {
        ArgumentNullException.ThrowIfNull(monitors);
        if (monitors.Count == 0)
        {
            throw new ArgumentException("At least one monitor is required.", nameof(monitors));
        }

        var root = new RootContainer();

        List<MonitorInfo> ordered = [.. OrderPrimaryFirst(monitors)];
        IReadOnlyList<IReadOnlyList<string>> plan = PlanWorkspaceNames(workspaces, ordered.Count);

        for (int i = 0; i < ordered.Count; i++)
        {
            var monitor = new Monitor(ordered[i].WorkArea);

            // this monitor's round robin slice, first append is active
            foreach (string name in plan[i])
            {
                monitor.AppendChild(new Workspace(name));
            }

            root.AppendChild(monitor);
        }

        // No Focus() needed: append order makes each monitor's first workspace
        // active, and the primary (appended first) the focused monitor
        return root;
    }

    /// <summary>
    /// The canonical monitor order the tree uses: primary first, then
    /// left-to-right (then top-down). Public so the status bar can pair its
    /// per-monitor windows with the tree's monitors by index.
    /// </summary>
    public static IEnumerable<MonitorInfo> OrderPrimaryFirst(IReadOnlyList<MonitorInfo> monitors) =>
        monitors
            .OrderByDescending(monitor => monitor.IsPrimary)
            .ThenBy(monitor => monitor.Bounds.X)
            .ThenBy(monitor => monitor.Bounds.Y);

    /// <summary>
    /// Per-monitor workspace-name assignment for
    /// <paramref name="monitorCount" /> monitors: element <c>i</c> is the name
    /// list for the monitor at canonical index <c>i</c> (see
    /// <see cref="OrderPrimaryFirst" />), sliced round-robin from
    /// <see cref="ResolveNames" />
    /// (<c>name[j] -> monitor[j % monitorCount]</c>).
    /// </summary>
    internal static IReadOnlyList<IReadOnlyList<string>> PlanWorkspaceNames(
        WorkspaceOptions? workspaces,
        int monitorCount
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(monitorCount);
        IReadOnlyList<string> names = ResolveNames(workspaces, monitorCount);

        var perMonitor = new List<IReadOnlyList<string>>(monitorCount);
        for (int i = 0; i < monitorCount; i++)
        {
            List<string> slice = [];
            for (int j = i; j < names.Count; j += monitorCount)
            {
                slice.Add(names[j]);
            }

            perMonitor.Add(slice);
        }

        return perMonitor;
    }

    private static IReadOnlyList<string> ResolveNames(
        WorkspaceOptions? workspaces,
        int monitorCount
    )
    {
        if (workspaces?.Names is { Count: > 0 } explicitNames)
        {
            if (explicitNames.Count < monitorCount)
            {
                throw new ArgumentException(
                    $"workspaces.names has {explicitNames.Count} entries but there are {monitorCount} monitors; provide at least one name per monitor."
                );
            }

            var seen = new HashSet<string>(explicitNames.Count, StringComparer.Ordinal);
            foreach (string name in explicitNames)
            {
                if (!seen.Add(name))
                {
                    throw new ArgumentException(
                        $"Duplicate workspace name detected: '{name}'. Workspace names must be unique."
                    );
                }
            }

            return explicitNames;
        }

        int perMonitor =
            workspaces?.PerMonitor is int count && count > 0 ? count : WorkspacesPerMonitor;
        int total = perMonitor * monitorCount;
        var generated = new List<string>(total);
        for (int number = 1; number <= total; number++)
        {
            generated.Add(number.ToString(CultureInfo.InvariantCulture));
        }

        return generated;
    }
}
