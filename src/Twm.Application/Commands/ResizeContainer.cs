using Twm.Application.Messaging;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Commands;

/// <summary>
/// Grows the focused window within its parent split by
/// <see cref="DeltaFraction" />, taking the same amount from an adjacent
/// sibling. Negative shrinks it.
/// </summary>
public sealed record ResizeContainerCommand(double DeltaFraction) : ICommand;

public sealed class ResizeContainerHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<ResizeContainerCommand>(root, layout)
{
    public override CommandResult Handle(ResizeContainerCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        TilingWindow? subject = Root.FocusedWindow();
        if (subject is null)
        {
            return CommandResult.Ok;
        }

        if (subject.ResizeWithNeighbor(command.DeltaFraction))
        {
            Rearrange();
        }

        return CommandResult.Ok;
    }
}
