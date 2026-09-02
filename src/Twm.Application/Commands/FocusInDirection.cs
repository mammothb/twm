using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Commands;

/// <summary>Moves focus to the adjacent window in a direction.</summary>
public sealed record FocusInDirectionCommand(Direction Direction) : ICommand;

public sealed class FocusInDirectionHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<FocusInDirectionCommand>(root, layout)
{
    public override CommandResult Handle(FocusInDirectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        TilingWindow? subject = Root.FocusedWindow();
        if (subject is null)
        {
            return CommandResult.Ok;
        }

        subject.FocusTargetInDirection(command.Direction)?.Focus();
        return CommandResult.Ok;
    }
}
