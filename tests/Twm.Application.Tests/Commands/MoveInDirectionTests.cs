using Twm.Application.Commands;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;
using Twm.TestSupport.Assertions;

namespace Twm.Application.Tests.Commands;

public class MoveInDirectionTests
{
    [Fact]
    public void Move_RightWithSibling_ReordersAndKeepsFocus()
    {
        // Arrange
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var left = new TilingWindow(new WindowId(1));
        var right = new TilingWindow(new WindowId(2));
        workspace.AppendChild(left);
        workspace.AppendChild(right);
        left.Focus();

        // Act
        new MoveInDirectionHandler(root, new LayoutEngine()).Handle(
            new MoveInDirectionCommand(Direction.Right)
        );

        // Assert
        workspace.Children.ShouldBe([right, left]);
        root.FocusedWindow.ShouldBeSameAs(left);
        left.Bounds.ShouldBe(new Rect(400, 0, 400, 600));
    }

    [Fact]
    public void Move_LeftAtEdge_KeepsOrder()
    {
        // Arrange
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var left = new TilingWindow(new WindowId(1));
        var right = new TilingWindow(new WindowId(2));
        workspace.AppendChild(left);
        workspace.AppendChild(right);
        left.Focus();

        // Act
        new MoveInDirectionHandler(root, new LayoutEngine()).Handle(
            new MoveInDirectionCommand(Direction.Left)
        );

        // Assert
        workspace.Children.ShouldBe([left, right]);
    }

    [Fact]
    public void Move_LeftFromNestedSplit_PopsAndFlattens()
    {
        // Arrange
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var left = new TilingWindow(new WindowId(1));
        workspace.AppendChild(left);
        var rightSplit = new SplitContainer(Layout.SplitVertical);
        workspace.AppendChild(rightSplit);
        var rightTop = new TilingWindow(new WindowId(2));
        var rightBottom = new TilingWindow(new WindowId(3));
        rightSplit.AppendChild(rightTop);
        rightSplit.AppendChild(rightBottom);
        rightTop.Focus();

        // Act
        new MoveInDirectionHandler(root, new LayoutEngine()).Handle(
            new MoveInDirectionCommand(Direction.Left)
        );

        // Assert
        const string expected =
            "Monitor [0,0 800x600]\n"
            + "  Workspace \"1\" Horizontal [0,0 800x600]\n"
            + "    Window #1 [0,0 266x600]\n"
            + "    Window #2 [266,0 266x600]\n"
            + "    Window #3 [532,0 268x600]\n";
        TreeRenderer.Render(monitor).ShouldBe(expected);
        root.FocusedWindow.ShouldBeSameAs(rightTop);
    }

    [Fact]
    public void Move_NestedWindowAtEdge_LeavesTreeIntact()
    {
        // Arrange — focused window is the rightmost in a vertical column;
        // moving right from a vertical column is a no-op (column is at the
        // workspace's right edge).
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);

        var innerSplit = new SplitContainer();
        workspace.AppendChild(innerSplit);

        var innerLeft = new TilingWindow(new WindowId(1));
        var innerRight = new TilingWindow(new WindowId(2));
        innerSplit.AppendChild(innerLeft);
        innerSplit.AppendChild(innerRight);

        innerRight.Focus();

        // Act
        new MoveInDirectionHandler(root, new LayoutEngine()).Handle(
            new MoveInDirectionCommand(Direction.Right)
        );

        // Assert
        innerSplit.Children.ShouldBe([innerLeft, innerRight]);
        workspace.Children.ShouldBe([innerSplit]);
    }

    [Fact]
    public void Move_RightIntoAdjacentSplit_EntersSplit()
    {
        // Arrange
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var left = new TilingWindow(new WindowId(1));
        workspace.AppendChild(left);
        var rightSplit = new SplitContainer(Layout.SplitVertical);
        workspace.AppendChild(rightSplit);
        var rightTop = new TilingWindow(new WindowId(2));
        var rightBottom = new TilingWindow(new WindowId(3));
        rightSplit.AppendChild(rightTop);
        rightSplit.AppendChild(rightBottom);
        left.Focus();

        // Act
        new MoveInDirectionHandler(root, new LayoutEngine()).Handle(
            new MoveInDirectionCommand(Direction.Right)
        );

        // Assert
        left.Parent.ShouldBeSameAs(rightSplit);
        rightSplit.Children.ShouldBe([left, rightTop, rightBottom]);
        root.FocusedWindow.ShouldBeSameAs(left);
    }

    [Fact]
    public void Move_NoFocusedWindow_IsNoOp()
    {
        // Arrange — root has no windows, so FocusedWindow is null.
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);

        // Act
        CommandResult result = new MoveInDirectionHandler(root, new LayoutEngine()).Handle(
            new MoveInDirectionCommand(Direction.Right)
        );

        // Assert
        result.Success.ShouldBeTrue();
        root.FocusedWindow.ShouldBeNull();
    }
}
