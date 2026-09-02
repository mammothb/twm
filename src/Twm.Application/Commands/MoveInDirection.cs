using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Commands;

/// <summary>
/// Moves the focused window one step in a direction, restructuring the tree
/// i3-style.
/// </summary>
public sealed record MoveInDirectionCommand(Direction Direction) : ICommand;

public sealed class MoveInDirectionHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<MoveInDirectionCommand>(root, layout)
{
    public override CommandResult Handle(MoveInDirectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        TilingWindow? subject = Root.FocusedWindow();
        if (subject is null)
        {
            return CommandResult.Ok;
        }

        if (
            subject.MoveInDirection(command.Direction)
            || subject.MoveToAdjacentMonitor(command.Direction)
        )
        {
            Rearrange();
        }
        return CommandResult.Ok;
    }
}
