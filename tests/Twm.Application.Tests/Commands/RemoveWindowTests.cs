using Twm.Application.Commands;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;
using Twm.TestSupport.Assertions;

namespace Twm.Application.Tests.Commands;

public class RemoveWindowTests
{
    [Fact]
    public void RemoveWindow_RelayoutsRemaining()
    {
        var root = new RootContainer();
        var monitorBounds = new Rect(0, 0, 800, 600);
        var monitor = new Monitor(monitorBounds);
        root.AppendChild(monitor);
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        var windowId = new WindowId(1);
        var w1 = new TilingWindow(windowId);
        var w2 = new TilingWindow(new WindowId(2));
        ws.AppendChild(w1);
        ws.AppendChild(w2);

        new RemoveWindowHandler(root, new LayoutEngine()).Handle(new RemoveWindowCommand(windowId));

        ws.Children.ShouldBe([w2]);
        w2.Bounds.ShouldBe(monitorBounds);
    }

    [Fact]
    public void RemoveWindow_FlattensNowSingleChildSplit()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        var w1 = new TilingWindow(new WindowId(1));
        ws.AppendChild(w1);
        var right = new SplitContainer(Layout.SplitVertical);
        ws.AppendChild(right);
        var windowId = new WindowId(2);
        var w2 = new TilingWindow(windowId);
        var w3 = new TilingWindow(new WindowId(3));
        right.AppendChild(w2);
        right.AppendChild(w3);

        new RemoveWindowHandler(root, new LayoutEngine()).Handle(new RemoveWindowCommand(windowId));

        string expected =
            "Monitor [0,0 800x600]\n"
            + "  Workspace \"1\" Horizontal [0,0 800x600]\n"
            + "    Window #1 [0,0 400x600]\n"
            + "    Window #3 [400,0 400x600]\n";
        TreeRenderer.Render(monitor).ShouldBe(expected);
    }

    [Fact]
    public void RemoveWindow_UnknownWindowIsNoOp()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        var w1 = new TilingWindow(new WindowId(1));
        ws.AppendChild(w1);

        CommandResult result = new RemoveWindowHandler(root, new LayoutEngine()).Handle(
            new RemoveWindowCommand(new WindowId(99))
        );

        result.Success.ShouldBeTrue();
        ws.Children.ShouldBe([w1]);
    }
}
