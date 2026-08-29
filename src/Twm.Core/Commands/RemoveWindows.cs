using Twm.Core.Bussing;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Commands;

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

        Container? parent = window.Parent;
        if (parent is null)
        {
            return CommandResult.Ok;
        }

        parent.RemoveChild(window);
        Cleanup(parent);
        Rearrange();
        return CommandResult.Ok;
    }
}
