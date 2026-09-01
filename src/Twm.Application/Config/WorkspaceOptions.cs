namespace Twm.Application.Config;

/// <summary>
/// Workspace layout options: a per-monitor count and/or an explicit ordered
/// name list, consumed by the desktop builder to name and allocate workspaces
/// per monitor. This is the neutral application-ring model; mapping YAML into
/// it is the config adapter's job.
/// </summary>
public sealed class WorkspaceOptions
{
    /// <summary>Workspaces created on each monitor (default 4).</summary>
    public int? PerMonitor { get; set; }

    /// <summary>
    /// Explicit workspace names, distributed round-robin across monitors.
    /// Overrides count.
    /// </summary>
    public List<string>? Names { get; set; }
}
