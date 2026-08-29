using Twm.Core.Geometry;

namespace Twm.Core.Tree;

/// <summary>
/// A node in the layout tree. Concrete kinds are <see cref="RootContainer" />,
/// <see cref="Monitor" />, <see cref="Workspace" />,
/// <see cref="SplitContainer" />, and <see cref="TilingWindow" />.
/// </summary>
public abstract class Container
{
    private readonly List<Container> _children = [];
    private readonly List<Container> _childFocusOrder = [];

    /// <summary>Stable identity for the lifetime of the container.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>The parent container, or null when detached or root.</summary>
    public Container? Parent { get; private set; }

    /// <summary>Child containers in layout order.</summary>
    public IReadOnlyList<Container> Children => _children;

    /// <summary>Children ordered most-recently-focused first.</summary>
    public IReadOnlyList<Container> ChildFocusOrder => _childFocusOrder;

    /// <summary>
    /// Computed screen rectangle. Populated by the layout engine.
    /// </summary>
    public Rect Bounds { get; set; }

    /// <summary>
    /// Size relative to tiling siblings within a split. Defaults to 1.
    /// </summary>
    public double SizeFraction { get; set; } = 1.0;

    /// <summary>
    /// The most-recently-focused child, or null when there are none.
    /// </summary>
    public Container? LastFocusedChild => _childFocusOrder.Count > 0 ? _childFocusOrder[0] : null;

    /// <summary>
    /// The deepest most-recently-focused descendant, reached by following
    /// <see cref="LastFocusedChild" /> downwards. Null when there are no
    /// children.
    /// </summary>
    public Container? LastFocusedDescendant
    {
        get
        {
            Container? node = LastFocusedChild;
            while (node?.LastFocusedChild is not null)
            {
                node = node.LastFocusedChild;
            }
            return node;
        }
    }

    /// <summary>
    /// This container's position among its parent's children.
    /// </summary>
    public int Index => Parent?._children.IndexOf(this) ?? 0;

    /// <summary>
    /// This container's position in its parent's focus order.
    /// </summary>
    public int FocusIndex => Parent?._childFocusOrder.IndexOf(this) ?? 0;

    /// <summary>The next sibling in layout order, or null.</summary>
    public Container? NextSibling => Parent?._children.ElementAtOrDefault(Index + 1);

    /// <summary>The previous sibling in layout order, or null.</summary>
    public Container? PreviousSibling => Index > 0 ? Parent?._children[Index - 1] : null;

    /// <summary>Siblings excluding this container.</summary>
    public IReadOnlyList<Container> Siblings =>
        Parent?._children.FindAll(child => !ReferenceEquals(child, this)) ?? [];

    /// <summary>Ancestors from the immediate parent up to the root.</summary>
    public IEnumerable<Container> Ancestors
    {
        get
        {
            Container? node = Parent;
            while (node is not null)
            {
                yield return node;
                node = node.Parent;
            }
        }
    }

    /// <summary>All descendants in breadth-first order.</summary>
    public IEnumerable<Container> Descendants
    {
        get
        {
            var queue = new Queue<Container>();
            foreach (Container child in _children)
            {
                queue.Enqueue(child);
            }

            while (queue.Count > 0)
            {
                Container curr = queue.Dequeue();
                yield return curr;
                foreach (Container child in curr._children)
                {
                    queue.Enqueue(child);
                }
            }
        }
    }

    /// <summary>
    /// Determines whether this container is an ancestor of the specified target
    /// container.
    /// </summary>
    public bool IsAncestorOf(Container? target)
    {
        if (target is null || ReferenceEquals(target, this))
        {
            return false;
        }

        Container? node = target.Parent;
        while (node is not null)
        {
            if (ReferenceEquals(node, this))
            {
                return true;
            }
            node = node.Parent;
        }

        return false;
    }

    /// <summary>Inserts a detached child at the given index.</summary>
    public void InsertChild(int index, Container child)
    {
        ArgumentNullException.ThrowIfNull(child);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _children.Count);
        if (ReferenceEquals(child, this) || child.IsAncestorOf(this))
        {
            throw new InvalidOperationException(
                "Cannot attach a container to itself or one of its descendants."
            );
        }
        if (child.Parent is not null)
        {
            throw new InvalidOperationException("Container is already attached to a parent.");
        }

        _children.Insert(index, child);
        _childFocusOrder.Add(child);
        child.Parent = this;
    }

    /// <summary>
    /// Replaces an existing child with a new child, preserving layout position
    /// and focus rank.
    /// </summary>
    public void ReplaceChild(Container oldChild, Container newChild)
    {
        ArgumentNullException.ThrowIfNull(oldChild);
        ArgumentNullException.ThrowIfNull(newChild);
        if (ReferenceEquals(oldChild, newChild))
        {
            return;
        }

        int childIndex = _children.IndexOf(oldChild);
        if (childIndex == -1)
        {
            throw new InvalidOperationException(
                "The container to replace is not a child of this container."
            );
        }

        if (ReferenceEquals(newChild, this) || newChild.IsAncestorOf(this))
        {
            throw new InvalidOperationException(
                "Cannot attach a container to itself or one of its descendats."
            );
        }

        if (newChild.Parent is not null)
        {
            throw new InvalidOperationException(
                "Replacement container is already attached to a parent."
            );
        }

        _children[childIndex] = newChild;

        int focusIndex = _childFocusOrder.IndexOf(oldChild);
        if (focusIndex == -1)
        {
            _childFocusOrder.Insert(0, newChild);
        }
        else
        {
            _childFocusOrder[focusIndex] = newChild;
        }

        oldChild.Parent = null;
        newChild.Parent = this;
    }

    /// <summary>Appends a detached child after the current children.</summary>
    public void AppendChild(Container child)
    {
        InsertChild(_children.Count, child);
    }

    /// <summary>Detaches a direct child from this container.</summary>
    public void RemoveChild(Container child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (!_children.Remove(child))
        {
            throw new InvalidOperationException("Container is not a child of this container.");
        }

        _childFocusOrder.Remove(child);
        child.Parent = null;
    }

    /// <summary>
    /// Reorders an existing child to a new position in layout order (focus
    /// order unchanged).
    /// </summary>
    public void MoveChildToIndex(Container child, int newIndex)
    {
        ArgumentNullException.ThrowIfNull(child);
        int curr = _children.IndexOf(child);
        if (curr < 0)
        {
            throw new InvalidOperationException("Container is not a child of this container.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(newIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(newIndex, _children.Count);
        _children.RemoveAt(curr);
        _children.Insert(newIndex, child);
    }

    /// <summary>
    /// Records this container as most-recently-focused along its whole
    /// ancestry, so that <see cref="LastFocusedDescendant" /> from any ancestor
    /// reaches it.
    /// </summary>
    public void Focus()
    {
        Container curr = this;
        while (curr.Parent is Container parent)
        {
            parent._childFocusOrder.Remove(curr);
            parent._childFocusOrder.Insert(0, curr);
            curr = parent;
        }
    }
}
