using Twm.Application.Messaging;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Commands;

/// <summary>Activates and focuses the named workspace.</summary>
public sealed record FocusWorkspaceCommand(string WorkspaceName) : ICommand;

public sealed class FocusWorkspaceHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<FocusWorkspaceCommand>(root, layout)
{
    public override CommandResult Handle(FocusWorkspaceCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Workspace? workspace = Root.FindWorkspace(command.WorkspaceName);
        if (workspace is null)
        {
            return CommandResult.Fail($"No workspace named '{command.WorkspaceName}'.");
        }

        Container target = workspace.LastFocusedDescendant ?? workspace;
        target.Focus();
        return CommandResult.Ok;
    }
}
