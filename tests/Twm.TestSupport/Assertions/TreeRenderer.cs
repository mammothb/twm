using System.Text;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.TestSupport.Assertions;

/// <summary>
/// Renders a container tree to a stable, indented text form for snapshot tests.
/// Hand-written and reflection-free. Lines are separated by '\n' so snapshots
/// are identical across platforms.
/// </summary>
public static class TreeRenderer
{
    public static string Render(Container root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var builder = new StringBuilder();
        Append(builder, root, 0);
        return builder.ToString();
    }

    private static void Append(StringBuilder builder, Container container, int depth)
    {
        builder.Append(' ', depth * 2);
        builder.Append(Describe(container));
        builder.Append('\n');

        foreach (Container child in container.Children)
        {
            Append(builder, child, depth + 1);
        }
    }

    private static string Describe(Container container) =>
        container switch
        {
            RootContainer => "Root",
            Monitor monitor => $"Monitor {FormatRect(monitor.Bounds)}",
            Workspace workspace =>
                $"Workspace \"{workspace.Name}\" {DescribeLayout(workspace.Layout)} {FormatRect(workspace.Bounds)}",
            SplitContainer split =>
                $"Split {DescribeLayout(split.Layout)} {FormatRect(split.Bounds)}",
            TilingWindow window => $"Window #{window.WindowId.Value} {FormatRect(window.Bounds)}",
            _ => container.GetType().Name,
        };

    // Split layouts keep their historic axis label ("Horizontal"/"Vertical") so
    // existing snapshots are stable; tabbed/stacked add their own labels.
    private static string DescribeLayout(Layout layout) =>
        layout switch
        {
            Layout.SplitHorizontal => "Horizontal",
            Layout.SplitVertical => "Vertical",
            Layout.Tabbed => "Tabbed",
            Layout.Stacked => "Stacked",
            _ => layout.ToString(),
        };

    private static string FormatRect(Rect rect) =>
        $"[{rect.X},{rect.Y} {rect.Width}x{rect.Height}]";
}
