namespace Twm.Layout;

/// <summary>
/// Node of the binary split tree (i3-style containers).
/// Pure data structure — no Win32 anywhere in this namespace.
/// </summary>
public abstract class LayoutNode
{
    /// <summary>Parent container, null for the tree root.</summary>
    public SplitContainer? Parent { get; internal set; }

    /// <summary>
    /// Relative share of the parent's space along its split axis.
    /// Only meaningful when Parent != null.
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>Rect assigned by the last <see cref="SplitTree.Apply"/> call.</summary>
    public Rect AssignedRect { get; internal set; }

    public abstract IEnumerable<WindowLeaf> Leaves();

    /// <summary>Ancestors, nearest container first.</summary>
    public IEnumerable<SplitContainer> Ancestors()
    {
        for (SplitContainer? node = Parent; node is not null; node = node.Parent)
        {
            yield return node;
        }
    }

    internal static bool Contains(LayoutNode outer, LayoutNode inner)
    {
        for (LayoutNode? n = inner; n is not null; n = n.Parent)
        {
            if (ReferenceEquals(n, outer))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>A managed window inside the tree.</summary>
public sealed class WindowLeaf : LayoutNode
{
    public required nint Hwnd { get; init; }

    public override IEnumerable<WindowLeaf> Leaves()
    {
        yield return this;
    }

    public override string ToString() => $"Leaf({Hwnd})";
}

/// <summary>A split container dividing space between N children.</summary>
public sealed class SplitContainer : LayoutNode
{
    private readonly List<LayoutNode> _children = [];

    /// <summary>True: children split horizontally (side by side).</summary>
    public bool Horizontal { get; set; }

    public IReadOnlyList<LayoutNode> Children => _children;

    public override IEnumerable<WindowLeaf> Leaves() => _children.SelectMany(c => c.Leaves());

    public override string ToString() => $"{(Horizontal ? "H" : "V")}split[{Children.Count}]";

    internal int IndexOf(LayoutNode node) => _children.IndexOf(node);

    /// <summary>Attaches a node as the last child (external tree building).</summary>
    public void Add(LayoutNode node)
    {
        node.Parent = this;
        _children.Add(node);
    }

    internal void InsertAt(int index, LayoutNode node)
    {
        node.Parent = this;
        _children.Insert(index, node);
    }

    internal void Remove(LayoutNode node)
    {
        _children.Remove(node);
        node.Parent = null;
    }

    internal void Replace(LayoutNode oldNode, LayoutNode newNode)
    {
        int index = _children.IndexOf(oldNode);
        _children[index] = newNode;
        oldNode.Parent = null;
        newNode.Parent = this;
    }

    internal void SetChild(int index, LayoutNode node) => _children[index] = node;
}
