using Twm.Application.Messaging;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Commands;

/// <summary>
/// Brings a new OS window under management on the given monitor.
/// </summary>
public sealed record AdoptWindowCommand(WindowId WindowId, Monitor Monitor, WindowId? Owner = null)
    : ICommand;

public sealed class AdoptWindowHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<AdoptWindowCommand>(root, layout)
{
    public override CommandResult Handle(AdoptWindowCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Workspace? workspace = command.Monitor.ActiveWorkspace();
        if (workspace is null)
        {
            return CommandResult.Fail("Monitor has no workspace to adopt into.");
        }

        TilingWindow window = workspace.Adopt(command.WindowId, command.Owner);
        window.Focus();
        Rearrange();
        return CommandResult.Ok;
    }
}
