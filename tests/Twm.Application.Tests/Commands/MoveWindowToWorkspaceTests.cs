using Twm.Application.Commands;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Commands;

public class MoveWindowToWorkspaceTests
{
    [Fact]
    public void MoveReordersWithinRowAndKeepsFocus()
    {
        var root = new RootContainer();
        var monitorBounds = new Rect(0, 0, 800, 600);
        var monitor = new Monitor(monitorBounds);
        root.AppendChild(monitor);
        var ws1 = new Workspace("1");
        var ws2 = new Workspace("2");
        monitor.AppendChild(ws1);
        monitor.AppendChild(ws2);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        ws1.AppendChild(w1);
        ws1.AppendChild(w2);
        w1.Focus();

        new MoveWindowToWorkspaceHandler(root, new LayoutEngine()).Handle(
            new MoveWindowToWorkspaceCommand("2")
        );

        w1.WorkspaceOf().ShouldBeSameAs(ws2);
        ws1.Children.ShouldBe([w2]);
        w1.Bounds.ShouldBe(monitorBounds);
    }

    [Fact]
    public void MoveToUnknownWorkspaceFails()
    {
        var root = new RootContainer();
        var monitorBounds = new Rect(0, 0, 800, 600);
        var monitor = new Monitor(monitorBounds);
        root.AppendChild(monitor);
        var ws1 = new Workspace("1");
        monitor.AppendChild(ws1);
        var w1 = new TilingWindow(new WindowId(1));
        ws1.AppendChild(w1);
        w1.Focus();

        CommandResult result = new MoveWindowToWorkspaceHandler(root, new LayoutEngine()).Handle(
            new MoveWindowToWorkspaceCommand("2")
        );

        result.Success.ShouldBeFalse();
        ws1.Children.ShouldBe([w1]);
    }
}
