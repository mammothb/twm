using Twm.Application.Commands;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Commands;

public class SetLayoutTests
{
    [Fact]
    public void SetLayout_Tabbed_SetsFocusedWindowsParentLayout()
    {
        // Arrange
        (RootContainer root, Workspace workspace, LayoutEngine engine) = Desktop(windowCount: 3);
        root.FindWindow(new WindowId(1))!.Focus();

        // Act
        new SetLayoutHandler(root, engine).Handle(new SetLayoutCommand(Layout.Tabbed));

        // Assert
        workspace.Layout.ShouldBe(Layout.Tabbed);
    }

    [Fact]
    public void Focus_RightInTabbedContainer_CyclesToNextTab()
    {
        // Arrange
        (RootContainer root, Workspace workspace, LayoutEngine engine) = Desktop(windowCount: 3);
        workspace.Layout = Layout.Tabbed;
        root.FindWindow(new WindowId(1))!.Focus();

        // Act
        new FocusInDirectionHandler(root, engine).Handle(
            new FocusInDirectionCommand(Direction.Right)
        );

        // Assert
        root.FocusedWindow!.WindowId.ShouldBe(new WindowId(2));
    }

    [Fact]
    public void Focus_DownInStackedContainer_CyclesToNextItem()
    {
        // Arrange
        (RootContainer root, Workspace workspace, LayoutEngine engine) = Desktop(windowCount: 3);
        workspace.Layout = Layout.Stacked;
        root.FindWindow(new WindowId(1))!.Focus();

        // Act
        new FocusInDirectionHandler(root, engine).Handle(
            new FocusInDirectionCommand(Direction.Down)
        );

        // Assert
        root.FocusedWindow!.WindowId.ShouldBe(new WindowId(2));
    }

    [Fact]
    public void ToggleSplit_FromTabbed_ExitsToHorizontalSplit()
    {
        // Arrange
        (RootContainer root, Workspace workspace, LayoutEngine engine) = Desktop(windowCount: 2);
        workspace.Layout = Layout.Tabbed;
        root.FindWindow(new WindowId(1))!.Focus();

        // Act
        new ToggleSplitDirectionHandler(root, engine).Handle(new ToggleSplitDirectionCommand());

        // Assert
        workspace.Layout.ShouldBe(Layout.SplitHorizontal);
    }

    [Fact]
    public void Move_RightInTabbedContainer_ReordersTabs()
    {
        // Arrange
        (RootContainer root, Workspace workspace, LayoutEngine engine) = Desktop(windowCount: 3);
        workspace.Layout = Layout.Tabbed;
        root.FindWindow(new WindowId(1))!.Focus();

        // Act
        new MoveInDirectionHandler(root, engine).Handle(
            new MoveInDirectionCommand(Direction.Right)
        );

        // Assert — w1 swaps past w2 → order becomes [w2, w1, w3]; focus follows w1.
        ((TilingWindow)workspace.Children[0]).WindowId.ShouldBe(new WindowId(2));
        ((TilingWindow)workspace.Children[1]).WindowId.ShouldBe(new WindowId(1));
        root.FocusedWindow!.WindowId.ShouldBe(new WindowId(1));
    }

    [Fact]
    public void SetLayout_NoFocusedWindow_IsNoOp()
    {
        // Arrange — workspace has no windows, so FocusedWindow is null.
        (RootContainer root, Workspace workspace, LayoutEngine engine) = Desktop(windowCount: 0);

        // Act
        CommandResult result = new SetLayoutHandler(root, engine).Handle(
            new SetLayoutCommand(Layout.Tabbed)
        );

        // Assert — workspace layout unchanged.
        result.Success.ShouldBeTrue();
        workspace.Layout.ShouldBe(Layout.SplitHorizontal);
    }

    private static (RootContainer Root, Workspace Workspace, LayoutEngine Engine) Desktop(
        int windowCount
    )
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        for (int i = 1; i <= windowCount; i++)
        {
            workspace.AppendChild(new TilingWindow(new WindowId(i)));
        }

        return (root, workspace, new LayoutEngine());
    }
}
