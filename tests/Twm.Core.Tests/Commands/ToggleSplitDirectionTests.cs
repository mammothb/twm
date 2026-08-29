using Twm.Core.Commands;
using Twm.Core.Geometry;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Tests.Commands;

public class ToggleSplitDirectionTests
{
    [Fact]
    public void TogglesParentSplitFromHorizontalToVertical()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        ws.AppendChild(w1);
        ws.AppendChild(w2);
        w1.Focus();

        new ToggleSplitDirectionHandler(root, new LayoutEngine()).Handle(
            new ToggleSplitDirectionCommand()
        );

        ws.Layout.ShouldBe(LayoutMode.SplitVertical);
        w1.Bounds.ShouldBe(new Rect(0, 0, 800, 300));
        w2.Bounds.ShouldBe(new Rect(0, 300, 800, 300));
    }
}
