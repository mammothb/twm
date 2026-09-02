namespace Twm.Adapters.Ipc;

/// <summary>
/// A serializable snapshot of one node in the container tree, for
/// <c>twm-msg get-tree</c>. Built by <see cref="TreeSnapshotMapper" /> and
/// serialized via the source-generated <see cref="TwmJsonContext" />.
/// </summary>
public sealed record TreeNode
{
    /// <summary>
    /// Node kind: <c>root</c>, <c>monitor</c>, <c>workspace</c>, <c>split</c>,
    /// or <c>window</c>.
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>Computed screen rectangle.</summary>
    public BoundsDto Bounds { get; init; }

    /// <summary>
    /// Tiling axis (<c>horizontal</c>/<c>vertical</c>) for split and workspace
    /// nodes; null otherwise.
    /// </summary>
    public string? Direction { get; init; }

    /// <summary>
    /// Layout (<c>splith</c>/<c>splitv</c>/<c>tabbed</c>/<c>stacked</c>) for
    /// split and workspace nodes; null otherwise.
    /// </summary>
    public string? Layout { get; init; }

    /// <summary>Workspace name; null for other kinds.</summary>
    public string? Name { get; init; }

    /// <summary>
    /// Native window id (HWND value) for window nodes; null otherwise.
    /// </summary>
    public long? WindowId { get; init; }

    /// <summary>
    /// Window title if a resolver was supplied; null otherwise.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>Size relative to tiling siblings within a split.</summary>
    public double SizeFraction { get; init; }

    /// <summary>True for the single globally-focused window.</summary>
    public bool Focused { get; init; }

    /// <summary>True for the active (shown) workspace of its monitor.</summary>
    public bool Active { get; init; }

    /// <summary>
    /// Child nodes in layout order; null when there are none.
    /// </summary>
    public IReadOnlyList<TreeNode>? Children { get; init; }
}

/// <summary>
/// A serializable rectangle mirroring <c>Twm.Domain.Geometry.Rect</c>.
/// </summary>
public readonly record struct BoundsDto(int X, int Y, int Width, int Height);
