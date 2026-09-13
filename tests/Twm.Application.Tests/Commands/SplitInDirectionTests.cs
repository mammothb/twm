using Twm.Application.Commands;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Commands;

public class SplitInDirectionTests
{
    [Fact]
    public void SplitVertical_WithSiblings_WrapsFocusedWindowInNestedSplit()
    {
        (RootContainer root, _, Workspace ws) = Desktop();
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        ws.AppendChild(w1);
        ws.AppendChild(w2);
        w1.Focus();

        new SplitInDirectionHandler(root, new LayoutEngine()).Handle(
            new SplitInDirectionCommand(TilingDirection.Vertical)
        );

        SplitContainer wrapper = w1.Parent.ShouldBeOfType<SplitContainer>();
        wrapper.Layout.ShouldBe(Layout.SplitVertical);
        // w1 is inside SplitContainer now
        wrapper.ShouldNotBeSameAs(ws);
        // SplitContainer parent is inside the workspace
        wrapper.Parent.ShouldBeSameAs(ws);
        ws.Children.Count.ShouldBe(2);
    }

    [Fact]
    public void Split_LoneWindow_JustReorientItsParent()
    {
        (RootContainer root, _, Workspace ws) = Desktop();
        var w1 = new TilingWindow(new WindowId(1));
        ws.AppendChild(w1);
        w1.Focus();

        new SplitInDirectionHandler(root, new LayoutEngine()).Handle(
            new SplitInDirectionCommand(TilingDirection.Vertical)
        );

        // No wrapper, the workspace itself is re-oriented, w1 stays a direct
        // child
        w1.Parent.ShouldBeSameAs(ws);
        ws.Layout.ShouldBe(Layout.SplitVertical);
    }

    [Fact]
    public void SplitThenAdopt_NestsTheNewWindowInsideTheSplit()
    {
        (RootContainer root, Monitor monitor, Workspace ws) = Desktop();
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        ws.AppendChild(w1);
        ws.AppendChild(w2);
        w1.Focus();
        var windowId = new WindowId(3);

        var layout = new LayoutEngine();
        new SplitInDirectionHandler(root, layout).Handle(
            new SplitInDirectionCommand(TilingDirection.Vertical)
        );
        // Adopt a new window, it opens next to the focused w1, i.e., inside the
        // wrapper
        new AdoptWindowHandler(root, layout).Handle(new AdoptWindowCommand(windowId, monitor));

        SplitContainer wrapper = w1.Parent.ShouldBeOfType<SplitContainer>();
        wrapper.Layout.ShouldBe(Layout.SplitVertical);
        // w1 + new window
        wrapper.Children.Count.ShouldBe(2);
        root.FindWindow(windowId)!.Parent.ShouldBeSameAs(wrapper);
    }

    private static (RootContainer Root, Monitor Monitor, Workspace Workspace) Desktop()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        return (root, monitor, ws);
    }
}
