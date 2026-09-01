using Twm.Application.Commands;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Commands;

public class ResizeInDirectionTests
{
    [Fact]
    public void ResizeInDirection_RightRowsFocusedWidth_TakingFromNeighbor()
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

        new ResizeInDirectionHandler(root, new LayoutEngine()).Handle(
            new ResizeInDirectionCommand(Direction.Right, 0.5)
        );

        // w1: 1.5 / 2.0 * 800 = 600. w2: 200
        w1.Bounds.ShouldBe(new Rect(0, 0, 600, 600));
        w2.Bounds.ShouldBe(new Rect(600, 0, 200, 600));
    }

    [Fact]
    public void ResizeInDirection_LeftShrinksFocusedWidth()
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

        new ResizeInDirectionHandler(root, new LayoutEngine()).Handle(
            new ResizeInDirectionCommand(Direction.Left, 0.5)
        );

        // w1: 0.5 / 2.0 * 800 = 200. w2: 600
        w1.Bounds.ShouldBe(new Rect(0, 0, 200, 600));
        w2.Bounds.ShouldBe(new Rect(200, 0, 600, 600));
    }

    [Fact]
    public void ResizeInDirection_RightFromInsideVerticalSplit_ResizesAncestorHorizontalSplit()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        var left = new SplitContainer(Layout.SplitVertical);
        ws.AppendChild(left);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        left.AppendChild(w1);
        left.AppendChild(w2);
        var w3 = new TilingWindow(new WindowId(3));
        ws.AppendChild(w3);
        w1.Focus();

        new ResizeInDirectionHandler(root, new LayoutEngine()).Handle(
            new ResizeInDirectionCommand(Direction.Right, 0.5)
        );

        // vertical column (holding w1, w2) grew to 600 wide, w3 shrank to 200
        left.Bounds.ShouldBe(new Rect(0, 0, 600, 600));
        w1.Bounds.ShouldBe(new Rect(0, 0, 600, 300));
        w3.Bounds.ShouldBe(new Rect(600, 0, 200, 600));
    }

    [Fact]
    public void ResizeInDirection_BelowMinimumFraction_IsNoOp()
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

        // would drive w1 to 0.05 < 0.1
        new ResizeInDirectionHandler(root, new LayoutEngine()).Handle(
            new ResizeInDirectionCommand(Direction.Left, 0.95)
        );

        w1.SizeFraction.ShouldBe(1.0);
        w2.SizeFraction.ShouldBe(1.0);
    }

    [Fact]
    public void ResizeInDirection_NoResizableAncestor_IsNoOp()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        var w1 = new TilingWindow(new WindowId(1));
        ws.AppendChild(w1);
        w1.Focus();

        new ResizeInDirectionHandler(root, new LayoutEngine()).Handle(
            new ResizeInDirectionCommand(Direction.Right, 0.5)
        );

        w1.SizeFraction.ShouldBe(1.0);
    }
}
