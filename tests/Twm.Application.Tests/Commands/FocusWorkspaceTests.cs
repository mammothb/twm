using Twm.Application.Commands;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Commands;

public class FocusWorkspaceTests
{
    [Fact]
    public void FocusingWorkspaceActivatesItAndFocusesItsWindow()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws1 = new Workspace("1");
        var ws2 = new Workspace("2");
        monitor.AppendChild(ws1);
        monitor.AppendChild(ws2);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        ws1.AppendChild(w1);
        ws2.AppendChild(w2);
        w1.Focus();

        new FocusWorkspaceHandler(root, new LayoutEngine()).Handle(new FocusWorkspaceCommand("2"));

        monitor.LastFocusedChild.ShouldBeSameAs(ws2);
        root.FocusedWindow().ShouldBeSameAs(w2);
    }

    [Fact]
    public void FocusingUnknownWorkspaceFails()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws1 = new Workspace("1");
        monitor.AppendChild(ws1);

        CommandResult result = new FocusWorkspaceHandler(root, new LayoutEngine()).Handle(
            new FocusWorkspaceCommand("2")
        );

        result.Success.ShouldBeFalse();
    }
}
