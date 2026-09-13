using Twm.Application.Commands;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Commands;

public class AdoptWindowTests
{
    [Fact]
    public void Adopt_EmptyWorkspace_FillsWorkspace()
    {
        // Arrange
        var root = new RootContainer();
        var monitorBounds = new Rect(0, 0, 800, 600);
        var monitor = new Monitor(monitorBounds);
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var windowId = new WindowId(1);

        // Act
        new AdoptWindowHandler(root, new LayoutEngine()).Handle(
            new AdoptWindowCommand(windowId, monitor)
        );

        // Assert
        TilingWindow? adopted = root.FindWindow(windowId);
        adopted.ShouldNotBeNull();
        adopted.Parent.ShouldBeSameAs(workspace);
        adopted.Bounds.ShouldBe(monitorBounds);
        root.FocusedWindow.ShouldBeSameAs(adopted);
    }

    [Fact]
    public void Adopt_WithFocusedSibling_OpensNextToFocused()
    {
        // Arrange
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        var existing = new TilingWindow(new WindowId(1));
        workspace.AppendChild(existing);
        existing.Focus();
        var windowId = new WindowId(2);

        // Act
        new AdoptWindowHandler(root, new LayoutEngine()).Handle(
            new AdoptWindowCommand(windowId, monitor)
        );

        // Assert
        TilingWindow? adopted = root.FindWindow(windowId);
        adopted.ShouldNotBeNull();
        workspace.Children.ShouldBe([existing, adopted]);
        adopted.Parent.ShouldBeSameAs(workspace);
        existing.Bounds.ShouldBe(new Rect(0, 0, 400, 600));
        adopted.Bounds.ShouldBe(new Rect(400, 0, 400, 600));
    }

    [Fact]
    public void Adopt_OnSecondMonitor_DoesNotAffectFirst()
    {
        // Arrange
        var root = new RootContainer();
        var primary = new Monitor(new Rect(0, 0, 800, 600));
        var secondary = new Monitor(new Rect(800, 0, 1024, 768));
        root.AppendChild(primary);
        root.AppendChild(secondary);
        var workspaceOnPrimary = new Workspace("1");
        var workspaceOnSecondary = new Workspace("2");
        primary.AppendChild(workspaceOnPrimary);
        secondary.AppendChild(workspaceOnSecondary);
        var onPrimary = new TilingWindow(new WindowId(1));
        workspaceOnPrimary.AppendChild(onPrimary);

        var layout = new LayoutEngine();
        layout.Arrange(root);
        var windowId = new WindowId(2);

        // Act
        new AdoptWindowHandler(root, layout).Handle(new AdoptWindowCommand(windowId, secondary));

        // Assert
        TilingWindow? adopted = root.FindWindow(windowId);
        adopted.ShouldNotBeNull();
        adopted.Parent.ShouldBeSameAs(workspaceOnSecondary);
        onPrimary.Bounds.ShouldBe(new Rect(0, 0, 800, 600));
        adopted.Bounds.ShouldBe(new Rect(800, 0, 1024, 768));
    }

    [Fact]
    public void Adopt_MonitorWithoutActiveWorkspace_Fails()
    {
        // Arrange — monitor has no workspace children at all.
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var windowId = new WindowId(1);

        // Act
        CommandResult result = new AdoptWindowHandler(root, new LayoutEngine()).Handle(
            new AdoptWindowCommand(windowId, monitor)
        );

        // Assert
        result.Success.ShouldBeFalse();
        root.FindWindow(windowId).ShouldBeNull();
    }
}
