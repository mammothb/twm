using Twm.Application.Commands;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Commands;

public class FocusWorkspaceTests
{
    [Fact]
    public void Focus_ExistingWorkspace_ActivatesAndFocusesWindow()
    {
        // Arrange
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var first = new Workspace("1");
        var second = new Workspace("2");
        monitor.AppendChild(first);
        monitor.AppendChild(second);
        var onFirst = new TilingWindow(new WindowId(1));
        var onSecond = new TilingWindow(new WindowId(2));
        first.AppendChild(onFirst);
        second.AppendChild(onSecond);
        onFirst.Focus();

        // Act
        new FocusWorkspaceHandler(root, new LayoutEngine()).Handle(new FocusWorkspaceCommand("2"));

        // Assert
        monitor.LastFocusedChild.ShouldBeSameAs(second);
        root.FocusedWindow.ShouldBeSameAs(onSecond);
    }

    [Fact]
    public void Focus_UnknownWorkspace_Fails()
    {
        // Arrange
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var first = new Workspace("1");
        monitor.AppendChild(first);

        // Act
        CommandResult result = new FocusWorkspaceHandler(root, new LayoutEngine()).Handle(
            new FocusWorkspaceCommand("2")
        );

        // Assert
        result.Success.ShouldBeFalse();
    }
}
