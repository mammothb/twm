using Twm.Application.Commands;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Commands;

public class ResizeContainerTests
{
    [Fact]
    public void ResizeContainer_RelayoutsRemaining()
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
        new ResizeContainerHandler(root, new LayoutEngine()).Handle(
            new ResizeContainerCommand(0.5)
        );

        // Assert — left: 1.5 / 2.0 * 800 = 600; right: 200.
        left.Bounds.ShouldBe(new Rect(0, 0, 600, 600));
        right.Bounds.ShouldBe(new Rect(600, 0, 200, 600));
    }

    [Fact]
    public void ResizeContainer_SingleWindowIsNoOp()
    {
        // Arrange
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var window = new TilingWindow(new WindowId(1));
        workspace.AppendChild(window);
        window.Focus();

        // Act
        new ResizeContainerHandler(root, new LayoutEngine()).Handle(
            new ResizeContainerCommand(0.5)
        );

        // Assert
        window.SizeFraction.ShouldBe(1.0);
    }

    [Fact]
    public void ResizeContainer_NoFocusedWindow_IsNoOp()
    {
        // Arrange — root has no windows, so FocusedWindow is null.
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);

        // Act
        CommandResult result = new ResizeContainerHandler(root, new LayoutEngine()).Handle(
            new ResizeContainerCommand(0.5)
        );

        // Assert
        result.Success.ShouldBeTrue();
        root.FocusedWindow.ShouldBeNull();
    }
}
