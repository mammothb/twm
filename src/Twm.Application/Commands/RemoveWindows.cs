using Twm.Application.Messaging;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Commands;

/// <summary>
/// Removes a managed window from the tree, e.g., when it is closed.
/// </summary>
public sealed record RemoveWindowCommand(WindowId WindowId) : ICommand;

public sealed class RemoveWindowHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<RemoveWindowCommand>(root, layout)
{
    public override CommandResult Handle(RemoveWindowCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        TilingWindow? window = Root.FindWindow(command.WindowId);
        if (window is null)
        {
            return CommandResult.Ok;
        }

        window.Remove();
        Rearrange();
        return CommandResult.Ok;
    }
}
