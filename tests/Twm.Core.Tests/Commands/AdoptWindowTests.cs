using Twm.Core.Commands;
using Twm.Core.Geometry;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Tests.Commands;

public class AdoptWindowTests
{
    [Fact]
    public void AdoptedWindowFillsAnEmptyWorkspace()
    {
        var root = new RootContainer();
        var monitorBounds = new Rect(0, 0, 800, 600);
        var monitor = new Monitor(monitorBounds);
        root.AppendChild(monitor);
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        var windowId = new WindowId(1);

        new AdoptWindowHandler(root, new LayoutEngine()).Handle(
            new AdoptWindowCommand(windowId, monitor)
        );

        TilingWindow? adopted = root.FindWindow(windowId);
        adopted.ShouldNotBeNull();
        adopted.Parent.ShouldBeSameAs(ws);
        adopted.Bounds.ShouldBe(monitorBounds);
        root.FocusedWindow().ShouldBeSameAs(adopted);
    }

    [Fact]
    public void AdoptedWindowOpensNextToFocusedWindow()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        var existing = new TilingWindow(new WindowId(1));
        ws.AppendChild(existing);
        existing.Focus();
        var windowId = new WindowId(2);

        new AdoptWindowHandler(root, new LayoutEngine()).Handle(
            new AdoptWindowCommand(windowId, monitor)
        );

        TilingWindow? adopted = root.FindWindow(windowId);
        adopted.ShouldNotBeNull();
        ws.Children.ShouldBe([existing, adopted]);
        adopted.Parent.ShouldBeSameAs(ws);
        existing.Bounds.ShouldBe(new Rect(0, 0, 400, 600));
        adopted.Bounds.ShouldBe(new Rect(400, 0, 400, 600));
    }

    [Fact]
    public void AdoptingOntoSecondMonitorLeavesFirstMonitorUntouched()
    {
        var root = new RootContainer();
        var primary = new Monitor(new Rect(0, 0, 800, 600));
        var secondary = new Monitor(new Rect(800, 0, 1024, 768));
        root.AppendChild(primary);
        root.AppendChild(secondary);
        var ws1 = new Workspace("1");
        var ws2 = new Workspace("2");
        primary.AppendChild(ws1);
        secondary.AppendChild(ws2);
        var onPrimary = new TilingWindow(new WindowId(1));
        ws1.AppendChild(onPrimary);

        var layout = new LayoutEngine();
        layout.Arrange(root);
        var windowId = new WindowId(2);

        new AdoptWindowHandler(root, layout).Handle(new AdoptWindowCommand(windowId, secondary));

        TilingWindow? adopted = root.FindWindow(windowId);
        adopted.ShouldNotBeNull();
        adopted.Parent.ShouldBeSameAs(ws2);
        onPrimary.Bounds.ShouldBe(new Rect(0, 0, 800, 600));
        adopted.Bounds.ShouldBe(new Rect(800, 0, 1024, 768));
    }
}
