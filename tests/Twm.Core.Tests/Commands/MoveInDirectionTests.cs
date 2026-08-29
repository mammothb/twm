using Twm.Core.Commands;
using Twm.Core.Geometry;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Tests.Commands;

public class MoveInDirectionTests
{
    [Fact]
    public void MoveReordersWithinRowAndKeepsFocus()
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

        new MoveInDirectionHandler(root, new LayoutEngine()).Handle(
            new MoveInDirectionCommand(Direction.Right)
        );

        ws.Children.ShouldBe([w2, w1]);
        root.FocusedWindow().ShouldBeSameAs(w1);
        w1.Bounds.ShouldBe(new Rect(400, 0, 400, 600));
    }

    [Fact]
    public void MoveAtEdgeIsNoOp()
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

        new MoveInDirectionHandler(root, new LayoutEngine()).Handle(
            new MoveInDirectionCommand(Direction.Left)
        );

        ws.Children.ShouldBe([w1, w2]);
    }

    [Fact]
    public void MovePopsOutOfNestedSplitAndFlattensIt()
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
        w2.Focus();

        new MoveInDirectionHandler(root, new LayoutEngine()).Handle(
            new MoveInDirectionCommand(Direction.Left)
        );

        string expected =
            "Monitor [0,0 800x600]\n"
            + "  Workspace \"1\" Horizontal [0,0 800x600]\n"
            + "    Window #1 [0,0 266x600]\n"
            + "    Window #2 [266,0 266x600]\n"
            + "    Window #3 [532,0 268x600]\n";
        TreeRenderer.Render(monitor).ShouldBe(expected);
        root.FocusedWindow().ShouldBeSameAs(w2);
    }

    [Fact]
    public void MoveNestedWindowAtEdgeDoesNotBreakTree()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws = new Workspace("1");
        monitor.AppendChild(ws);

        var innerSplit = new SplitContainer();
        ws.AppendChild(innerSplit);

        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        innerSplit.AppendChild(w1);
        innerSplit.AppendChild(w2);

        w2.Focus();

        // Moving Right from w2 (where ReferenceEquals(pivot, subject) is false
        // for ws, and inBounds is false for ws because innerSplit is the
        // rightmost child of ws)
        new MoveInDirectionHandler(root, new LayoutEngine()).Handle(
            new MoveInDirectionCommand(Direction.Right)
        );

        // w2 stays in innerSplit because there's nowhere to go in ws.
        innerSplit.Children.ShouldBe([w1, w2]);
        ws.Children.ShouldBe([innerSplit]);
    }

    [Fact]
    public void MoveIntoAdjacentSplit()
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
        w1.Focus();

        new MoveInDirectionHandler(root, new LayoutEngine()).Handle(
            new MoveInDirectionCommand(Direction.Right)
        );

        w1.Parent.ShouldBeSameAs(right);
        right.Children.ShouldBe([w1, w2, w3]);
        root.FocusedWindow().ShouldBeSameAs(w1);
    }
}
