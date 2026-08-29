using Twm.Core.Commands;
using Twm.Core.Geometry;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Tests.Commands;

public class FocusInDirectionTests
{
    [Fact]
    public void FocusMovesToRightNeighbor()
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

        new FocusInDirectionHandler(root, new LayoutEngine()).Handle(
            new FocusInDirectionCommand(Direction.Right)
        );

        root.FocusedWindow().ShouldBeSameAs(w2);
    }

    [Fact]
    public void FocusAtEdgeIsNoOp()
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

        new FocusInDirectionHandler(root, new LayoutEngine()).Handle(
            new FocusInDirectionCommand(Direction.Left)
        );

        root.FocusedWindow().ShouldBeSameAs(w1);
    }

    [Fact]
    public void FocusEntersAdjacentSplitAtItsLastFocusedWindow()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        var w1 = new TilingWindow(new WindowId(1));
        ws.AppendChild(w1);
        var right = new SplitContainer(LayoutMode.SplitVertical);
        ws.AppendChild(right);
        var w2 = new TilingWindow(new WindowId(2));
        var w3 = new TilingWindow(new WindowId(3));
        right.AppendChild(w2);
        right.AppendChild(w3);

        w3.Focus(); // make w3 last-focused inside the right split
        w1.Focus(); // not focus w1 on the left

        new FocusInDirectionHandler(root, new LayoutEngine()).Handle(
            new FocusInDirectionCommand(Direction.Right)
        );

        root.FocusedWindow().ShouldBeSameAs(w3);
    }
}
