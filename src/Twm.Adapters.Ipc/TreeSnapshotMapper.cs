using System.Text.Json;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Adapters.Ipc;

/// <summary>
/// Maps the live container tree to a serializable <see cref="TreeNode" /> and
/// renders it to a single-line JSON string for <c>twm-msg get-tree</c>. The
/// optional title resolver lets the live WM attach window titles the core tree
/// does not store.
/// </summary>
public static class TreeSnapshotMapper
{
    /// <summary>
    /// Builds a snapshot of the whole tree rooted at <paramref name="root" />.
    /// </summary>
    public static TreeNode From(RootContainer root, Func<WindowId, string?>? titleOf = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        TilingWindow? focused = root.FocusedWindow();
        return ToNode(root, focused, titleOf);
    }

    public static string ToJson(RootContainer root, Func<WindowId, string?>? titleOf = null) =>
        JsonSerializer.Serialize(From(root, titleOf), TwmJsonContext.Default.TreeNode);

    private static TreeNode ToNode(
        Container container,
        TilingWindow? focused,
        Func<WindowId, string?>? titleOf
    )
    {
        List<TreeNode>? children = null;
        if (container.Children.Count > 0)
        {
            children = new List<TreeNode>(container.Children.Count);
            foreach (Container child in container.Children)
            {
                children.Add(ToNode(child, focused, titleOf));
            }
        }

        return new TreeNode
        {
            Kind = MapKind(container),
            Bounds = MapBounds(container.Bounds),
            Direction = MapDirection(container),
            Layout = MapLayout(container),
            Name = (container as Workspace)?.Name,
            WindowId = container is TilingWindow idWindow ? (long)idWindow.WindowId.Value : null,
            Title = container is TilingWindow titleWindow
                ? titleOf?.Invoke(titleWindow.WindowId)
                : null,
            SizeFraction = container.SizeFraction,
            Focused = container is TilingWindow window && ReferenceEquals(window, focused),
            Active =
                container is Workspace workspace
                && container.Parent is Monitor monitor
                && ReferenceEquals(monitor.LastFocusedChild, workspace),
            Children = children,
        };
    }

    private static BoundsDto MapBounds(Rect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);

    private static string? MapDirection(Container container) =>
        container is SplitContainer split
            ? split.Layout.Axis().ToString().ToLowerInvariant()
            : null;

    private static string MapKind(Container container) =>
        container switch
        {
            RootContainer => "root",
            Monitor => "monitor",
            Workspace => "workspace",
            SplitContainer => "split",
            TilingWindow => "window",
            _ => "container",
        };

    private static string? MapLayout(Container container) =>
        container is SplitContainer split
            ? split.Layout switch
            {
                Layout.SplitHorizontal => "splith",
                Layout.SplitVertical => "splitv",
                Layout.Tabbed => "tabbed",
                Layout.Stacked => "stacked",
                _ => null,
            }
            : null;
}
