namespace Twm.Core.Tree;

/// <summary>
/// The root split of a monitor's layout tree. Identified by <see cref="Name" />,
/// e.g., "1".
/// </summary>
public sealed class Workspace(string name, LayoutMode layout = LayoutMode.SplitHorizontal)
    : SplitContainer(layout)
{
    /// <summary>Display name of number, e.g., "1".</summary>
    public string Name { get; } = name;
}
