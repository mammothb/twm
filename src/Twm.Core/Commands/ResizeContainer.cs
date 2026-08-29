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
        if (subject is null || subject.Parent is not Container parent)
        {
            return CommandResult.Ok;
        }

        Container? neighbor = subject.NextSibling ?? subject.PreviousSibling;
        if (neighbor is null)
        {
            return CommandResult.Ok;
        }

        double totalWeight = 0;
        for (int i = 0; i < parent.Children.Count; i++)
        {
            totalWeight += parent.Children[i].SizeFraction;
        }
        double newSubject = subject.SizeFraction + command.DeltaFraction;
        double newNeighbor = neighbor.SizeFraction - command.DeltaFraction;
        if (
            newSubject / totalWeight < MinimumFraction
            || newNeighbor / totalWeight < MinimumFraction
        )
        {
            return CommandResult.Ok;
        }

        subject.SizeFraction = newSubject;
        neighbor.SizeFraction = newNeighbor;
        Rearrange();
        return CommandResult.Ok;
    }
}
