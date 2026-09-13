using Twm.Application.Commands;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Commands;

public class FocusInDirectionTests
{
    [Fact]
    public void Focus_RightWithNeighbor_FocusesRightNeighbor()
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
        new FocusInDirectionHandler(root, new LayoutEngine()).Handle(
            new FocusInDirectionCommand(Direction.Right)
        );

        // Assert
        root.FocusedWindow.ShouldBeSameAs(right);
    }

    [Fact]
    public void Focus_LeftAtEdge_KeepsFocus()
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
        new FocusInDirectionHandler(root, new LayoutEngine()).Handle(
            new FocusInDirectionCommand(Direction.Left)
        );

        // Assert
        root.FocusedWindow.ShouldBeSameAs(left);
    }

    [Fact]
    public void Focus_RightIntoAdjacentSplit_FocusesLastDescendant()
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

        rightBottom.Focus(); // make rightBottom last-focused inside the right split
        left.Focus(); // then focus left, so the command targets left

        // Act
        new FocusInDirectionHandler(root, new LayoutEngine()).Handle(
            new FocusInDirectionCommand(Direction.Right)
        );

        // Assert
        root.FocusedWindow.ShouldBeSameAs(rightBottom);
    }

    [Fact]
    public void Focus_NoFocusedWindow_IsNoOp()
    {
        // Arrange — root has no windows, so FocusedWindow is null.
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);

        // Act
        CommandResult result = new FocusInDirectionHandler(root, new LayoutEngine()).Handle(
            new FocusInDirectionCommand(Direction.Right)
        );

        // Assert
        result.Success.ShouldBeTrue();
        root.FocusedWindow.ShouldBeNull();
    }
}
