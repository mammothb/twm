using Twm.Application.Commands;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Commands;

public class ToggleSplitDirectionTests
{
    [Fact]
    public void ToggleSplit_HorizontalParent_TogglesToVertical()
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
        new ToggleSplitDirectionHandler(root, new LayoutEngine()).Handle(
            new ToggleSplitDirectionCommand()
        );

        // Assert
        workspace.Layout.ShouldBe(Layout.SplitVertical);
        left.Bounds.ShouldBe(new Rect(0, 0, 800, 300));
        right.Bounds.ShouldBe(new Rect(0, 300, 800, 300));
    }

    [Fact]
    public void ToggleSplit_NoFocusedWindow_IsNoOp()
    {
        // Arrange — root has no windows, so FocusedWindow is null.
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        Layout originalLayout = workspace.Layout;

        // Act
        CommandResult result = new ToggleSplitDirectionHandler(root, new LayoutEngine()).Handle(
            new ToggleSplitDirectionCommand()
        );

        // Assert
        result.Success.ShouldBeTrue();
        workspace.Layout.ShouldBe(originalLayout);
    }
}
