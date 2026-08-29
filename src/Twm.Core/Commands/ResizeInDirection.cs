using Twm.Core.Bussing;
using Twm.Core.Geometry;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Commands;

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
    private const double MinimumFraction = 0.1;

    public override CommandResult Handle(ResizeInDirectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(command.DeltaFraction);
        TilingWindow? subject = Root.FocusedWindow();
        if (subject is null)
        {
            return CommandResult.Ok;
        }

        TilingDirection axis = command.Direction.Axis();
        bool grow = command.Direction is Direction.Right or Direction.Down;
        double delta = grow ? command.DeltaFraction : -command.DeltaFraction;

        // Walk up to the nearest ancestor split on the matching axis whose
        // child on the subject's path has a neighbor to trade size with
        Container pivot = subject;
        while (pivot.Parent is SplitContainer split)
        {
            if (split.Layout.Axis() == axis && TryResize(pivot, delta))
            {
                Rearrange();
                return CommandResult.Ok;
            }
            pivot = split;
        }
        return CommandResult.Ok;
    }

    private static bool TryResize(Container pivot, double delta)
    {
        Container? neighbor = pivot.NextSibling ?? pivot.PreviousSibling;
        if (neighbor is null)
        {
            return false;
        }

        double newPivot = pivot.SizeFraction + delta;
        double newNeighbor = neighbor.SizeFraction - delta;
        if (newPivot < MinimumFraction || newNeighbor < MinimumFraction)
        {
            return false;
        }

        pivot.SizeFraction = newPivot;
        neighbor.SizeFraction = newNeighbor;
        return true;
    }
}
