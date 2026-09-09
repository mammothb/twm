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
public abstract class TreeCommandHandler<TCommand>(RootContainer root, LayoutEngine engine)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected RootContainer Root { get; } = root;

    protected LayoutEngine Engine { get; } = engine;

    public abstract CommandResult Handle(TCommand command);

    /// <summary>
    /// Recomputes bounds for the whole tree after a mutation.
    /// </summary>
    protected void Rearrange() => Engine.Arrange(Root);
}
