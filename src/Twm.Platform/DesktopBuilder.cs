using System.Globalization;
using Twm.Core.Tree;
using Twm.Platform.Config;

namespace Twm.Platform;

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
        WorkspacesDto? workspaces = null
    )
    {
        ArgumentNullException.ThrowIfNull(monitors);
        if (monitors.Count == 0)
        {
            throw new ArgumentException("At least one monitor is required.", nameof(monitors));
        }

        var root = new RootContainer();

        List<MonitorInfo> ordered = OrderPrimaryFirst(monitors).ToList();
        int monitorCount = ordered.Count;
        IReadOnlyList<string> names = ResolveNames(workspaces, monitorCount);

        for (int i = 0; i < monitorCount; i++)
        {
            var monitor = new Monitor(ordered[i].WorkArea);

            // Round-robin: this monitor gets names at indices index,
            // index+monitorCount, ... (preserving order), so the first appended
            // is its active workspace
            for (int j = i; j < names.Count; j += monitorCount)
            {
                monitor.AppendChild(new Workspace(names[j]));
            }

            root.AppendChild(monitor);
        }

        // No explicit Focus() needed: append order already makes each monitor's
        // first workspace its active (LastFocused) child, and the primary
        // (appended first) the focused monitor
        return root;
    }

    private static IReadOnlyList<string> ResolveNames(WorkspacesDto? workspaces, int monitorCount)
    {
        if (workspaces?.Names is { Count: > 0 } explicitNames)
        {
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

            if (explicitNames.Count < monitorCount)
            {
                throw new ArgumentException(
                    $"workspaces.names has {explicitNames.Count} entries but there are {monitorCount} monitors; provide at least one name per monitor."
                );
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

    /// <summary>
    /// The canonical monitor order the tree uses: primary first, then
    /// left-to-right (then top-down). Public so the status bar can pair its
    /// per-monitor windows with the tree's monitors by index.
    /// </summary>
    public static IEnumerable<MonitorInfo> OrderPrimaryFirst(IReadOnlyList<MonitorInfo> monitors)
    {
        return monitors
            .OrderByDescending(monitor => monitor.IsPrimary)
            .ThenBy(monitor => monitor.Bounds.X)
            .ThenBy(monitor => monitor.Bounds.Y);
    }
}
