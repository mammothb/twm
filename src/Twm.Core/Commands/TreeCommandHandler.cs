using Twm.Core.Bussing;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Commands;

/// <summary>
/// Base for command handlers that mutate the container tree. Holds the tree
/// root and the layout engine, and provides shared re-arrange and tree-cleanup
/// helpers.
/// </summary>
public abstract class TreeCommandHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected RootContainer Root { get; }
    protected LayoutEngine Layout { get; }

#pragma warning disable IDE0290 // Abstract types should not have public constructors
    protected TreeCommandHandler(RootContainer root, LayoutEngine layout)
    {
        Root = root;
        Layout = layout;
    }
#pragma warning restore IDE0290

    public abstract CommandResult Handle(TCommand command);

    /// <summary>
    /// Recomputes bounds for the whole tree after a mutation.
    /// </summary>
    protected void Rearrange()
    {
        Layout.Arrange(Root);
    }

    /// <summary>
    /// Walks up from <paramref name="start" /> removing empty splits and
    /// flattening single-child splits (the lone child takes the split's place
    /// and size). Never removes or flattens a workspace.
    /// </summary>
    protected static void Cleanup(Container? start)
    {
        Container? node = start;
        while (node is SplitContainer split and not Workspace && split.Parent is Container parent)
        {
            if (split.Children.Count == 0)
            {
                parent.RemoveChild(split);
                node = parent;
            }
            else if (split.Children.Count == 1)
            {
                Container onlyChild = split.Children[0];
                int index = split.Index;
                double fraction = split.SizeFraction;
                split.RemoveChild(onlyChild);
                parent.RemoveChild(split);
                onlyChild.SizeFraction = fraction;
                parent.InsertChild(index, onlyChild);
                node = parent;
            }
            else
            {
                return;
            }
        }
    }
}
