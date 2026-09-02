using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Commands;

/// <summary>
/// Creates a nested split of the given direction around the focused window
/// (i3's <c>split</c>), so the next window opened or moved next to it nests
/// inside that split. If the focused window is the only child of its parent
/// split, that split is simply re-oriented instead of wrapped.
/// </summary>
public sealed record SplitDirectionCommand(TilingDirection Direction) : ICommand;

public sealed class SplitDirectionHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<SplitDirectionCommand>(root, layout)
{
    public override CommandResult Handle(SplitDirectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (Root.FocusedWindow() is not { Parent: SplitContainer } subject)
        {
            return CommandResult.Ok;
        }

        subject.SplitInDirection(command.Direction);
        Rearrange();
        return CommandResult.Ok;
    }
}
