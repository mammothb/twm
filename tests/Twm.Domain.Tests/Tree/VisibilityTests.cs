using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Domain.Tests.Tree;

public class VisibilityTests
{
    private static Monitor SingleMonitor()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        return monitor;
    }

    [Fact]
    public void DetachedWindow_IsNotVisible()
    {
        var window = new TilingWindow(new WindowId(1));

        window.IsEffectivelyVisible().ShouldBeFalse();
    }

    [Fact]
    public void WindowOnActiveWorkspace_IsVisible()
    {
        Monitor monitor = SingleMonitor();
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        var window = new TilingWindow(new WindowId(1));
        ws.AppendChild(window);

        window.IsEffectivelyVisible().ShouldBeTrue();
    }

    [Fact]
    public void WindowOnInactiveWorkspace_IsNotVisible()
    {
        Monitor monitor = SingleMonitor();
        var ws = new Workspace("1", Layout.Tabbed);
        monitor.AppendChild(ws);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        ws.AppendChild(w1);
        ws.AppendChild(w2);

        // w1 is first added, so should be focused by default
        w1.IsEffectivelyVisible().ShouldBeTrue();
        w2.IsEffectivelyVisible().ShouldBeFalse();
    }

    [Fact]
    public void FocusingAStackedChild_FlipsVisibility()
    {
        Monitor monitor = SingleMonitor();
        var ws = new Workspace("1", Layout.Stacked);
        monitor.AppendChild(ws);

        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        ws.AppendChild(w1);
        ws.AppendChild(w2);

        w2.Focus();

        w1.IsEffectivelyVisible().ShouldBeFalse();
        w2.IsEffectivelyVisible().ShouldBeTrue();
    }
}
