using Twm.Core.Bussing;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Commands;

/// <summary>
/// Brings a new OS window under management on the given monitor.
/// </summary>
public sealed record AdoptWindowCommand(WindowId WindowId, Monitor Monitor) : ICommand;

public sealed class AdoptWindowHandler(RootContainer root, LayoutEngine layout)
    : TreeCommandHandler<AdoptWindowCommand>(root, layout)
{
    public override CommandResult Handle(AdoptWindowCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Workspace? workspace = FirstWorkspace(command.Monitor);
        if (workspace is null)
        {
            return CommandResult.Fail("Monitor has no workspace to adopt into.");
        }

        var window = new TilingWindow(command.WindowId);

        // Open next to the workspace's focused window, i3-style; otherwise fill
        // the workspace
        if (
            workspace.LastFocusedDescendant is TilingWindow focused
            && focused.Parent is SplitContainer parent
        )
        {
            parent.InsertChild(focused.Index + 1, window);
        }
        else
        {
            workspace.AppendChild(window);
        }

        window.Focus();
        Rearrange();
        return CommandResult.Ok;
    }

    private static Workspace? FirstWorkspace(Monitor monitor)
    {
        if (monitor.LastFocusedChild is Workspace active)
        {
            return active;
        }

        foreach (Container child in monitor.Children)
        {
            if (child is Workspace workspace)
            {
                return workspace;
            }
        }
        return null;
    }
}
