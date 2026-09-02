using Twm.Application.Messaging;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Commands;

/// <summary>
/// Base for command handlers that mutate the container tree. Holds the tree
/// root and the layout engine, and re-arranges after a mutation. The
/// tree-restructuring operations themselves live in the domain
/// <see cref="TreeMutations" />.
/// </summary>
public abstract class TreeCommandHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
#pragma warning disable IDE0290 // Abstract types should not have public constructors
    protected TreeCommandHandler(RootContainer root, LayoutEngine layout)
    {
        Root = root;
        Layout = layout;
    }
#pragma warning restore IDE0290

    protected RootContainer Root { get; }
    protected LayoutEngine Layout { get; }

    public abstract CommandResult Handle(TCommand command);

    /// <summary>
    /// Recomputes bounds for the whole tree after a mutation.
    /// </summary>
    protected void Rearrange()
    {
        Layout.Arrange(Root);
    }
}
