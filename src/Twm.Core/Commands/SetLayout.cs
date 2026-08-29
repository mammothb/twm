using Twm.Core.Bussing;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Commands;

/// <summary>
/// Sets the layout of the focused window's parent container (i3's
/// <c>layout</c>), e.g., <c>layout tabbed</c> makes the container holding the
/// window tabbed.
/// </summary>
public sealed record SetLayoutCommand(LayoutMode Layout) : ICommand;

public sealed class SetLayoutHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<SetLayoutCommand>(root, layout)
{
    public override CommandResult Handle(SetLayoutCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (Root.FocusedWindow()?.Parent is not SplitContainer parent)
        {
            return CommandResult.Ok;
        }

        parent.Layout = command.Layout;
        Rearrange();
        return CommandResult.Ok;
    }
}
