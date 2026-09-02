using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Commands;

/// <summary>
/// Resizes the focused window along the axis of <see cref="Direction" />:
/// Right/Down grow it, Left/Up shrink it, by <see cref="DeltaFraction" />.
/// Unlike <see cref="ResizeContainerCommand" /> (which resizes only within the
/// immediate parent split), this walks up to the nearest ancestor split on the
/// matching axis, so "grow width" works even when the focused window sits
/// inside a vertical column (it resizes that column against its horizontal
/// neighbor).
/// </summary>
public sealed record ResizeInDirectionCommand(Direction Direction, double DeltaFraction) : ICommand;

public sealed class ResizeInDirectionHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<ResizeInDirectionCommand>(root, layout)
{
    public override CommandResult Handle(ResizeInDirectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(command.DeltaFraction);
        TilingWindow? subject = Root.FocusedWindow();
        if (subject is null)
        {
            return CommandResult.Ok;
        }

        if (subject.ResizeInDirection(command.Direction, command.DeltaFraction))
        {
            Rearrange();
        }

        return CommandResult.Ok;
    }
}
