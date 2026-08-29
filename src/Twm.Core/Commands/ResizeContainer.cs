using Twm.Core.Bussing;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Commands;

/// <summary>
/// Grows the focused window within its parent split by
/// <see cref="DeltaFraction" />, taking the same amount from an adjacent
/// sibling. Negative shrinks it.
/// </summary>
public sealed record ResizeContainerCommand(double DeltaFraction) : ICommand;

public sealed class ResizeContainerHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<ResizeContainerCommand>(root, layout)
{
    private const double MinimumFraction = 0.1;

    public override CommandResult Handle(ResizeContainerCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        TilingWindow? subject = Root.FocusedWindow();
        if (subject is null)
        {
            return CommandResult.Ok;
        }

        Container? neighbor = subject.NextSibling ?? subject.PreviousSibling;
        if (neighbor is null)
        {
            return CommandResult.Ok;
        }

        double newSubject = subject.SizeFraction + command.DeltaFraction;
        double newNeighbor = neighbor.SizeFraction + command.DeltaFraction;
        if (newSubject < MinimumFraction || newNeighbor < MinimumFraction)
        {
            return CommandResult.Ok;
        }

        subject.SizeFraction = newSubject;
        neighbor.SizeFraction = newNeighbor;
        Rearrange();
        return CommandResult.Ok;
    }
}
