using Twm.Application.Messaging;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Commands;

/// <summary>
/// Toggles the tiling direction of the focused window's parent split.
/// </summary>
public sealed record ToggleSplitDirectionCommand : ICommand;

public sealed class ToggleSplitDirectionHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<ToggleSplitDirectionCommand>(root, layout)
{
    public override CommandResult Handle(ToggleSplitDirectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (Root.FocusedWindow()?.Parent is not SplitContainer split)
        {
            return CommandResult.Ok;
        }

        split.ToggleSplitDirection();
        Rearrange();
        return CommandResult.Ok;
    }
}
