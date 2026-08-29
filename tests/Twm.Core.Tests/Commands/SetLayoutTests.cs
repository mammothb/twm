using Twm.Core.Commands;
using Twm.Core.Geometry;
using Twm.Core.Layout;
using Twm.Core.Tree;

namespace Twm.Core.Tests.Commands;

public class SetLayoutTests
{
    [Fact]
    public void SetLayout_Tabbed_SetsFocusedWindowsParentLayout()
    {
        (RootContainer root, Workspace ws, LayoutEngine layout) = Desktop(3);
        root.FindWindow(new WindowId(1))!.Focus();

        new SetLayoutHandler(root, layout).Handle(new SetLayoutCommand(LayoutMode.Tabbed));

        ws.Layout.ShouldBe(LayoutMode.Tabbed);
    }

    [Fact]
    public void FocusRight_InTabbedContainer_CyclesToNextTab()
    {
        (RootContainer root, Workspace ws, LayoutEngine layout) = Desktop(3);
        ws.Layout = LayoutMode.Tabbed;
        root.FindWindow(new WindowId(1))!.Focus();

        new FocusInDirectionHandler(root, layout).Handle(
            new FocusInDirectionCommand(Direction.Right)
        );

        root.FocusedWindow()!.WindowId.ShouldBe(new WindowId(2));
    }

    [Fact]
    public void FocusDown_InStackedContainer_CyclesToNextItem()
    {
        (RootContainer root, Workspace ws, LayoutEngine layout) = Desktop(3);
        ws.Layout = LayoutMode.Stacked;
        root.FindWindow(new WindowId(1))!.Focus();

        new FocusInDirectionHandler(root, layout).Handle(
            new FocusInDirectionCommand(Direction.Down)
        );

        root.FocusedWindow()!.WindowId.ShouldBe(new WindowId(2));
    }

    [Fact]
    public void ToggleSplit_FromTabbed_ExistsToHorizontalSplit()
    {
        (RootContainer root, Workspace ws, LayoutEngine layout) = Desktop(2);
        ws.Layout = LayoutMode.Tabbed;
        root.FindWindow(new WindowId(1))!.Focus();

        new ToggleSplitDirectionHandler(root, layout).Handle(new ToggleSplitDirectionCommand());

        ws.Layout.ShouldBe(LayoutMode.SplitHorizontal);
    }

    [Fact]
    public void MoveRight_InTabbedContainer_ReordersTabs()
    {
        (RootContainer root, Workspace ws, LayoutEngine layout) = Desktop(3);
        ws.Layout = LayoutMode.Tabbed;
        root.FindWindow(new WindowId(1))!.Focus();

        new MoveInDirectionHandler(root, layout).Handle(
            new MoveInDirectionCommand(Direction.Right)
        );

        // w1 swaps past w2 -> order becomes [w2, w1, w3], focus follows w1
        ((TilingWindow)ws.Children[0]).WindowId.ShouldBe(new WindowId(2));
        ((TilingWindow)ws.Children[1]).WindowId.ShouldBe(new WindowId(1));
        root.FocusedWindow()!.WindowId.ShouldBe(new WindowId(1));
    }

    private static (RootContainer Root, Workspace Workspace, LayoutEngine Layout) Desktop(
        int numWindows
    )
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 800, 600));
        root.AppendChild(monitor);
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        for (int i = 1; i <= numWindows; i++)
        {
            ws.AppendChild(new TilingWindow(new WindowId(i)));
        }
        return (root, ws, new LayoutEngine());
    }
}
