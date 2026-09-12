using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Domain.Tests.Tree;

public class MonitorActiveWorkspaceTests
{
    [Fact]
    public void ActiveWorkspace_WhenLastFocusedChildIsWorkspace_ReturnsIt()
    {
        Monitor monitor = BuildMonitor();
        Workspace ws = monitor.Children.OfType<Workspace>().Last();
        ws.Focus();

        monitor.ActiveWorkspace.ShouldBeSameAs(ws);
    }

    [Fact]
    public void ActiveWorkspace_WhenLastFocusedChildIsNotWorkspace_ReturnsFirstWorkspace()
    {
        var monitor = new Monitor(new Rect(0, 0, 100, 100));
        var split = new SplitContainer();
        split.Focus();
        var ws = new Workspace("1");
        monitor.AppendChild(split);
        monitor.AppendChild(ws);

        monitor.ActiveWorkspace.ShouldBeSameAs(ws);
    }

    [Fact]
    public void ActiveWorkspace_WhenNoWorkspaces_ReturnsNull()
    {
        var monitor = new Monitor(new Rect(0, 0, 100, 100));

        monitor.ActiveWorkspace.ShouldBeNull();
    }

    private static Monitor BuildMonitor()
    {
        var monitor = new Monitor(new Rect(0, 0, 100, 100));
        monitor.AppendChild(new Workspace("1"));
        monitor.AppendChild(new Workspace("2"));
        return monitor;
    }
}
