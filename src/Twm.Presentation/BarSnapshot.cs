namespace Twm.Presentation;

/// <summary>One workspace chip on a monitor's bar.</summary>
public sealed record WorkspaceItem(string Name, bool Active, bool Occupied);

/// <summary>
/// What one monitor's bar show. <see cref="Index" /> is the monitor's order in
/// the tree (primary-first, then left-to-right); the renderer pairs it with the
/// monitor of the same index to the bar window.
/// </summary>
public sealed record MonitorBarView(
    int Index,
    IReadOnlyList<WorkspaceItem> Workspaces,
    string? FocusedTitle
);

/// <summary>The complete render model for all bars at a moment.</summary>
public sealed record BarSnapshot(IReadOnlyList<MonitorBarView> Monitors, string Clock);
