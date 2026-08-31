namespace Twm.Domain.Tree;

/// <summary>
/// The root split of a monitor's layout tree. Identified by
/// <see cref="Name" />, e.g., "1".
/// </summary>
public sealed class Workspace : SplitContainer
{
    public Workspace(string name, Layout layout = Layout.SplitHorizontal)
        : base(layout)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    /// <summary>Display name of number, e.g., "1".</summary>
    public string Name { get; }
}
