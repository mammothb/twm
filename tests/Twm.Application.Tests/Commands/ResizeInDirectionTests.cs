using Twm.Application.Commands;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Commands;

public class ResizeInDirectionTests
{
    [Fact]
    public void ResizeInDirection_RightGrowsFocusedWidth_TakingFromNeighbor()
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
        new ResizeInDirectionHandler(root, new LayoutEngine()).Handle(
            new ResizeInDirectionCommand(Direction.Right, 0.5)
        );

        // Assert — left: 1.5 / 2.0 * 800 = 600; right: 200.
        left.Bounds.ShouldBe(new Rect(0, 0, 600, 600));
        right.Bounds.ShouldBe(new Rect(600, 0, 200, 600));
    }

    [Fact]
    public void ResizeInDirection_LeftShrinksFocusedWidth()
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
        new ResizeInDirectionHandler(root, new LayoutEngine()).Handle(
            new ResizeInDirectionCommand(Direction.Left, 0.5)
        );

        // Assert — left: 0.5 / 2.0 * 800 = 200; right: 600.
        left.Bounds.ShouldBe(new Rect(0, 0, 200, 600));
        right.Bounds.ShouldBe(new Rect(200, 0, 600, 600));
    }

    [Fact]
    public void ResizeInDirection_RightFromInsideVerticalSplit_ResizesAncestorHorizontalSplit()
    {
        // Arrange
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var leftColumn = new SplitContainer(Layout.SplitVertical);
        workspace.AppendChild(leftColumn);
        var top = new TilingWindow(new WindowId(1));
        var bottom = new TilingWindow(new WindowId(2));
        leftColumn.AppendChild(top);
        leftColumn.AppendChild(bottom);
        var rightColumn = new TilingWindow(new WindowId(3));
        workspace.AppendChild(rightColumn);
        top.Focus();

        // Act
        new ResizeInDirectionHandler(root, new LayoutEngine()).Handle(
            new ResizeInDirectionCommand(Direction.Right, 0.5)
        );

        // Assert — left column grew to 600 wide, right column shrank to 200.
        leftColumn.Bounds.ShouldBe(new Rect(0, 0, 600, 600));
        top.Bounds.ShouldBe(new Rect(0, 0, 600, 300));
        rightColumn.Bounds.ShouldBe(new Rect(600, 0, 200, 600));
    }

    [Fact]
    public void ResizeInDirection_BelowMinimumFraction_IsNoOp()
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

        // Act — would drive left to 0.05, below the 0.1 minimum.
        new ResizeInDirectionHandler(root, new LayoutEngine()).Handle(
            new ResizeInDirectionCommand(Direction.Left, 0.95)
        );

        // Assert
        left.SizeFraction.ShouldBe(1.0);
        right.SizeFraction.ShouldBe(1.0);
    }

    [Fact]
    public void ResizeInDirection_NoResizableAncestor_IsNoOp()
    {
        // Arrange — single window, no sibling to take from.
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var window = new TilingWindow(new WindowId(1));
        workspace.AppendChild(window);
        window.Focus();

        // Act
        new ResizeInDirectionHandler(root, new LayoutEngine()).Handle(
            new ResizeInDirectionCommand(Direction.Right, 0.5)
        );

        // Assert
        window.SizeFraction.ShouldBe(1.0);
    }

    [Fact]
    public void ResizeInDirection_NoFocusedWindow_IsNoOp()
    {
        // Arrange — root has no windows, so FocusedWindow is null.
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);

        // Act
        CommandResult result = new ResizeInDirectionHandler(root, new LayoutEngine()).Handle(
            new ResizeInDirectionCommand(Direction.Right, 0.5)
        );

        // Assert
        result.Success.ShouldBeTrue();
        root.FocusedWindow.ShouldBeNull();
    }
}
