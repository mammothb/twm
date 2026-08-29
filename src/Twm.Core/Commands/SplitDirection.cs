using Twm.Core.Bussing;
using Twm.Core.Geometry;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Commands;

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

        TilingWindow? subject = Root.FocusedWindow();
        if (subject?.Parent is not SplitContainer parent)
        {
            return CommandResult.Ok;
        }

        // A lone window: just set its parent split's direction (i3 splits a
        // solitary window by re-orienting its container rather than nesting a
        // redundant single-child split
        if (parent.Children.Count == 1)
        {
            parent.Layout = ToSplitLayout(command.Direction);
            Rearrange();
            return CommandResult.Ok;
        }

        // Otherwise wrap the focused window in a new split; the next window
        // inserted next to it will nest inside
        int index = subject.Index;
        double fraction = subject.SizeFraction;

        var wrapper = new SplitContainer(ToSplitLayout(command.Direction));
        parent.RemoveChild(subject);
        subject.SizeFraction = 1.0;
        wrapper.AppendChild(subject);
        wrapper.SizeFraction = fraction;
        parent.InsertChild(index, wrapper);
        subject.Focus();
        Rearrange();
        return CommandResult.Ok;
    }

    private static LayoutMode ToSplitLayout(TilingDirection direction)
    {
        return direction == TilingDirection.Vertical
            ? LayoutMode.SplitVertical
            : LayoutMode.SplitHorizontal;
    }
}
