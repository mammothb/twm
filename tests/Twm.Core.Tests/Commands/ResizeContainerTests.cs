using Twm.Core.Commands;
using Twm.Core.Geometry;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Tests.Commands;

public class ResizeContainerTests
{
    [Fact]
    public void ResizeContainer_RelayoutsRemaining()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        ws.AppendChild(w1);
        ws.AppendChild(w2);
        w1.Focus();

        new ResizeContainerHandler(root, new LayoutEngine()).Handle(
            new ResizeContainerCommand(0.5)
        );

        // w1: 1.5 / 2.0 * 800 = 600. w2: 200
        w1.Bounds.ShouldBe(new Rect(0, 0, 600, 600));
        w2.Bounds.ShouldBe(new Rect(600, 0, 200, 600));
    }

    [Fact]
    public void ResizeContainer_SingleWindowIsNoOp()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        var w1 = new TilingWindow(new WindowId(1));
        ws.AppendChild(w1);
        w1.Focus();

        new ResizeContainerHandler(root, new LayoutEngine()).Handle(
            new ResizeContainerCommand(0.5)
        );

        w1.SizeFraction.ShouldBe(1.0);
    }
}
