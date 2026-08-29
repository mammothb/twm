using Twm.Core.Bussing;
using Twm.Core.Geometry;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Commands;

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

        // Flip split-horizontal <-> split-vertical; from tabbed/stacked, exit
        // to a horizontal split (i3's "layout toggle split").
        split.Layout = split.Layout switch
        {
            LayoutMode.SplitHorizontal => LayoutMode.SplitVertical,
            LayoutMode.SplitVertical => LayoutMode.SplitHorizontal,
            _ => LayoutMode.SplitHorizontal,
        };
        Rearrange();
        return CommandResult.Ok;
    }
}
