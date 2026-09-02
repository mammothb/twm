using Twm.Application.Messaging;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Commands;

/// <summary>Moves the focused window to the named workspace.</summary>
public sealed record MoveWindowToWorkspaceCommand(string WorkspaceName) : ICommand;

public sealed class MoveWindowToWorkspaceHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<MoveWindowToWorkspaceCommand>(root, layout)
{
    public override CommandResult Handle(MoveWindowToWorkspaceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        TilingWindow? subject = Root.FocusedWindow();
        if (subject is null)
        {
            return CommandResult.Ok;
        }

        Workspace? target = Root.FindWorkspace(command.WorkspaceName);
        if (target is null)
        {
            return CommandResult.Fail($"No workspace named '{command.WorkspaceName}'.");
        }

        if (subject.MoveToWorkspace(target))
        {
            Rearrange();
        }

        return CommandResult.Ok;
    }
}
