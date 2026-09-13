using Twm.Application.Commands;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Commands;

public class MoveWindowToWorkspaceTests
{
    [Fact]
    public void MoveWindowToWorkspace_ExistingWorkspace_MovesAndRefocuses()
    {
        // Arrange
        var root = new RootContainer();
        var monitorBounds = new Rect(0, 0, 800, 600);
        var monitor = new Monitor(monitorBounds);
        root.AppendChild(monitor);
        var first = new Workspace("1");
        var second = new Workspace("2");
        monitor.AppendChild(first);
        monitor.AppendChild(second);
        var moved = new TilingWindow(new WindowId(1));
        var sibling = new TilingWindow(new WindowId(2));
        first.AppendChild(moved);
        first.AppendChild(sibling);
        moved.Focus();

        // Act
        new MoveWindowToWorkspaceHandler(root, new LayoutEngine()).Handle(
            new MoveWindowToWorkspaceCommand("2")
        );

        // Assert
        moved.FindAncestor<Workspace>().ShouldBeSameAs(second);
        first.Children.ShouldBe([sibling]);
        moved.Bounds.ShouldBe(monitorBounds);
    }

    [Fact]
    public void MoveWindowToWorkspace_UnknownWorkspace_Fails()
    {
        // Arrange
        var root = new RootContainer();
        var monitorBounds = new Rect(0, 0, 800, 600);
        var monitor = new Monitor(monitorBounds);
        root.AppendChild(monitor);
        var first = new Workspace("1");
        monitor.AppendChild(first);
        var focused = new TilingWindow(new WindowId(1));
        first.AppendChild(focused);
        focused.Focus();

        // Act
        CommandResult result = new MoveWindowToWorkspaceHandler(root, new LayoutEngine()).Handle(
            new MoveWindowToWorkspaceCommand("2")
        );

        // Assert
        result.Success.ShouldBeFalse();
        first.Children.ShouldBe([focused]);
    }

    [Fact]
    public void MoveWindowToWorkspace_NoFocusedWindow_IsNoOp()
    {
        // Arrange — root has no windows, so FocusedWindow is null.
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var first = new Workspace("1");
        var second = new Workspace("2");
        monitor.AppendChild(first);
        monitor.AppendChild(second);

        // Act
        CommandResult result = new MoveWindowToWorkspaceHandler(root, new LayoutEngine()).Handle(
            new MoveWindowToWorkspaceCommand("2")
        );

        // Assert
        result.Success.ShouldBeTrue();
        first.Children.ShouldBeEmpty();
        second.Children.ShouldBeEmpty();
    }
}
