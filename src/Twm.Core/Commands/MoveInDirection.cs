using Twm.Core.Bussing;
using Twm.Core.Geometry;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Commands;

/// <summary>
/// Moves the focused window one step in a direction, restructuring the tree
/// i3-style.
/// </summary>
public sealed record MoveInDirectionCommand(Direction Direction) : ICommand;

public sealed class MoveInDirectionHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<MoveInDirectionCommand>(root, layout)
{
    public override CommandResult Handle(MoveInDirectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        TilingWindow? subject = Root.FocusedWindow();
        if (subject is null)
        {
            return CommandResult.Ok;
        }

        if (
            TryMove(subject, command.Direction)
            || TryMoveAdjacentMonitor(subject, command.Direction)
        )
        {
            Rearrange();
        }
        return CommandResult.Ok;
    }

    private static bool TryMove(TilingWindow subject, Direction direction)
    {
        if (subject.Parent is not SplitContainer subjectParent)
        {
            return false;
        }

        TilingDirection axis = direction.Axis();
        int delta = direction is Direction.Left or Direction.Up ? -1 : 1;

        // Walk up to the nearest ancestor split whose orientation matches the
        // move axis. `pivot` is that split's child on the subject's path.
        Container pivot = subject;
        while (pivot.Parent is SplitContainer split)
        {
            if (split.Layout.Axis() == axis)
            {
                int neighborIndex = pivot.Index + delta;
                bool inBounds = 0 <= neighborIndex && neighborIndex < split.Children.Count;

                if (ReferenceEquals(pivot, subject))
                {
                    if (inBounds)
                    {
                        Container neighbor = split.Children[neighborIndex];
                        if (neighbor is SplitContainer nested && nested.Children.Count > 0)
                        {
                            // Move into the adjacent split at its near edge
                            subjectParent.RemoveChild(subject);
                            int insertAt = delta > 0 ? 0 : nested.Children.Count;
                            nested.InsertChild(insertAt, subject);
                            Cleanup(subjectParent);
                        }
                        else
                        {
                            // Reorder within the same split
                            split.MoveChildToIndex(subject, neighborIndex);
                        }

                        subject.Focus();
                        return true;
                    }
                }
                else if (inBounds)
                {
                    // Subject is nested deeper: pop it out beside its pivot
                    // branch
                    subjectParent.RemoveChild(subject);
                    int insertAt = delta > 0 ? pivot.Index + 1 : pivot.Index;
                    split.InsertChild(insertAt, subject);
                    Cleanup(subjectParent);
                    subject.Focus();
                    return true;
                }
            }
            pivot = split;
        }
        return false;
    }

    // At a workspace edge, move the window into the adjacent monitor's active
    // workspace, inserting at the near edge (Insert semantics), and follow it
    // with focus
    private static bool TryMoveAdjacentMonitor(TilingWindow subject, Direction direction)
    {
        if (
            subject.MonitorOf()?.AdjacentMonitor(direction)?.LastFocusedChild
                is not SplitContainer targetWorkspace
            || subject.Parent is not Container oldParent
        )
        {
            return false;
        }

        int delta = direction is Direction.Left or Direction.Up ? -1 : 1;
        oldParent.RemoveChild(subject);
        int insertAt = delta > 0 ? 0 : targetWorkspace.Children.Count;
        targetWorkspace.InsertChild(insertAt, subject);
        Cleanup(oldParent);
        subject.Focus();
        return true;
    }
}
