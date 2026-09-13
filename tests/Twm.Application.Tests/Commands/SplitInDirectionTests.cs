using Twm.Application.Commands;
using Twm.Application.Messaging;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Commands;

public class SplitInDirectionTests
{
    [Fact]
    public void Split_VerticalWithSibling_WrapsFocusedInNestedSplit()
    {
        // Arrange
        (RootContainer root, _, Workspace workspace) = Desktop();
        var left = new TilingWindow(new WindowId(1));
        var right = new TilingWindow(new WindowId(2));
        workspace.AppendChild(left);
        workspace.AppendChild(right);
        left.Focus();

        // Act
        new SplitInDirectionHandler(root, new LayoutEngine()).Handle(
            new SplitInDirectionCommand(TilingDirection.Vertical)
        );

        // Assert — left is now inside a fresh vertical wrapper, which sits
        // alongside right inside the workspace.
        SplitContainer wrapper = left.Parent.ShouldBeOfType<SplitContainer>();
        wrapper.Layout.ShouldBe(Layout.SplitVertical);
        wrapper.ShouldNotBeSameAs(workspace);
        wrapper.Parent.ShouldBeSameAs(workspace);
        workspace.Children.Count.ShouldBe(2);
    }

    [Fact]
    public void Split_VerticalWithLoneWindow_ReorientsParent()
    {
        // Arrange — single window; splitting it just flips the parent's
        // orientation, no wrapper needed.
        (RootContainer root, _, Workspace workspace) = Desktop();
        var window = new TilingWindow(new WindowId(1));
        workspace.AppendChild(window);
        window.Focus();

        // Act
        new SplitInDirectionHandler(root, new LayoutEngine()).Handle(
            new SplitInDirectionCommand(TilingDirection.Vertical)
        );

        // Assert — workspace stays the direct parent and is now vertical.
        window.Parent.ShouldBeSameAs(workspace);
        workspace.Layout.ShouldBe(Layout.SplitVertical);
    }

    [Fact]
    public void Split_VerticalThenAdopted_NestsNewWindowInSplit()
    {
        // Arrange — split first, then adopt a new window; the new window
        // should land inside the wrapper next to the focused window.
        (RootContainer root, Monitor monitor, Workspace workspace) = Desktop();
        var first = new TilingWindow(new WindowId(1));
        var second = new TilingWindow(new WindowId(2));
        workspace.AppendChild(first);
        workspace.AppendChild(second);
        first.Focus();
        var adoptedId = new WindowId(3);

        var layout = new LayoutEngine();
        new SplitInDirectionHandler(root, layout).Handle(
            new SplitInDirectionCommand(TilingDirection.Vertical)
        );
        new AdoptWindowHandler(root, layout).Handle(new AdoptWindowCommand(adoptedId, monitor));

        // Assert
        SplitContainer wrapper = first.Parent.ShouldBeOfType<SplitContainer>();
        wrapper.Layout.ShouldBe(Layout.SplitVertical);
        wrapper.Children.Count.ShouldBe(2);
        root.FindWindow(adoptedId)!.Parent.ShouldBeSameAs(wrapper);
    }

    [Fact]
    public void Split_NoFocusedWindow_IsNoOp()
    {
        // Arrange — root has no windows, so FocusedWindow is null.
        (RootContainer root, _, Workspace workspace) = Desktop();
        Layout originalLayout = workspace.Layout;

        // Act
        CommandResult result = new SplitInDirectionHandler(root, new LayoutEngine()).Handle(
            new SplitInDirectionCommand(TilingDirection.Vertical)
        );

        // Assert
        result.Success.ShouldBeTrue();
        workspace.Layout.ShouldBe(originalLayout);
        workspace.Children.ShouldBeEmpty();
    }

    private static (RootContainer Root, Monitor Monitor, Workspace Workspace) Desktop()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var workspace = new Workspace("1");
        monitor.AppendChild(workspace);
        return (root, monitor, workspace);
    }
}
