using Twm.Core.Bussing;
using Twm.Core.Geometry;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Commands;

/// <summary>Moves focus to the adjacent window in a direction.</summary>
public sealed record FocusInDirectionCommand(Direction Direction) : ICommand;

public sealed class FocusInDirectionHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<FocusInDirectionCommand>(root, layout)
{
    public override CommandResult Handle(FocusInDirectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        TilingWindow? subject = Root.FocusedWindow();
        if (subject is null)
        {
            return CommandResult.Ok;
        }

        Container? target =
            FindTarget(subject, command.Direction)
            ?? CrossMonitorTarget(subject, command.Direction);

        target?.Focus();
        return CommandResult.Ok;
    }

    private static Container? FindTarget(Container subject, Direction direction)
    {
        TilingDirection axis = direction.Axis();
        int delta = direction is Direction.Left or Direction.Up ? -1 : 1;

        Container node = subject;
        while (node.Parent is SplitContainer split)
        {
            if (split.Layout.Axis() == axis)
            {
                int neighborIndex = node.Index + delta;
                if (0 <= neighborIndex && neighborIndex < split.Children.Count)
                {
                    return DeepestFocusable(split.Children[neighborIndex]);
                }
            }
            node = split;
        }
        return null;
    }

    // At a workspace edge, fall through to the adjacent monitor's active
    // workspace and focus the window nearest the entry edge (moving right ->
    // its leftmost window), aligned to where we came from. This is spatially
    // consistent, unlike focusing whatever was last active there. Empty
    // target -> focus the workspace itself.
    private static Container? CrossMonitorTarget(Container subject, Direction direction)
    {
        Container? activeWorkspace = subject
            .MonitorOf()
            ?.AdjacentMonitor(direction)
            ?.LastFocusedChild;

        return activeWorkspace?.EdgeWindow(direction, subject.Bounds.Center) ?? activeWorkspace;
    }

    private static Container DeepestFocusable(Container node)
    {
        if (node is TilingWindow)
        {
            return node;
        }
        return node.LastFocusedDescendant ?? node;
    }
}
