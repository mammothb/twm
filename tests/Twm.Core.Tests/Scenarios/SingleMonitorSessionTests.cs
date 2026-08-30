using Twm.Core.Commands;
using Twm.Core.Geometry;
using Twm.Core.Tests.Fixtures;
using Twm.Core.Tree;

namespace Twm.Core.Tests.Scenarios;

public class SingleMonitorSessionTests
{
    [Fact]
    public void AdoptMoveAndResizeReshapeTheWorkspace()
    {
        var world = new TestWorld();

        world.Adopt(1, world.Primary);
        world.Adopt(2, world.Primary);
        world.Adopt(3, world.Primary); // [1, 2, 3], focus on 3
        world.Invoke(new MoveInDirectionCommand(Direction.Left)); // [1, 3, 2]
        world.Invoke(new ResizeContainerCommand(0.5));

        string expected =
            "Workspace \"1\" Horizontal [0,0 1920x1080]\n"
            + "  Window #1 [0,0 640x1080]\n"
            + "  Window #3 [640,0 960x1080]\n"
            + "  Window #2 [1600,0 320x1080]\n";

        TreeRenderer.Render(TestWorld.WorkspaceOf(world.Primary, 0)).ShouldBe(expected);
    }

    [Fact]
    public void WorkspaceSwitchingKeepsWindowsSeparated()
    {
        var world = new TestWorld(primaryWorkspaces: ["1", "2"], secondaryWorkspaces: ["3"]);

        world.Adopt(1, world.Primary);
        world.Invoke(new FocusWorkspaceCommand("2"));
        world.Adopt(2, world.Primary);

        Workspace ws1 = TestWorld.WorkspaceOf(world.Primary, 0);
        Workspace ws2 = TestWorld.WorkspaceOf(world.Primary, 1);
        ws1.Children.ShouldBe([world.Window(1)]);
        ws2.Children.ShouldBe([world.Window(2)]);
        world.Primary.LastFocusedChild.ShouldBeSameAs(ws2);
        world.Root.FocusedWindow().ShouldBeSameAs(world.Window(2));

        world.Invoke(new FocusWorkspaceCommand("1"));
        world.Primary.LastFocusedChild.ShouldBeSameAs(ws1);
        world.Root.FocusedWindow().ShouldBeSameAs(world.Window(1));
    }

    [Fact]
    public void MovingPopsAWindowOutOfANestedSplit()
    {
        var world = new TestWorld();
        Workspace ws = TestWorld.WorkspaceOf(world.Primary, 0);

        var w1 = new TilingWindow(new WindowId(1));
        ws.AppendChild(w1);
        var right = new SplitContainer(LayoutMode.SplitVertical);
        ws.AppendChild(right);
        var w2 = new TilingWindow(new WindowId(2));
        var w3 = new TilingWindow(new WindowId(3));
        right.AppendChild(w2);
        right.AppendChild(w3);
        w2.Focus();

        world.Invoke(new MoveInDirectionCommand(Direction.Left));

        string expected =
            "Workspace \"1\" Horizontal [0,0 1920x1080]\n"
            + "  Window #1 [0,0 640x1080]\n"
            + "  Window #2 [640,0 640x1080]\n"
            + "  Window #3 [1280,0 640x1080]\n";

        TreeRenderer.Render(ws).ShouldBe(expected);
        world.Root.FocusedWindow().ShouldBeSameAs(w2);
    }

    [Fact]
    public void MovingWindowToAnotherWorkspaceEmptiesTheSource()
    {
        var world = new TestWorld(primaryWorkspaces: ["1", "2"], secondaryWorkspaces: ["3"]);

        world.Adopt(1, world.Primary);
        world.Adopt(2, world.Primary);
        world.Invoke(new MoveWindowToWorkspaceCommand("2"));

        Workspace ws1 = TestWorld.WorkspaceOf(world.Primary, 0);
        Workspace ws2 = TestWorld.WorkspaceOf(world.Primary, 1);
        ws1.Children.ShouldBe([world.Window(1)]);
        ws2.Children.ShouldBe([world.Window(2)]);
        world.Window(2).WorkspaceOf().ShouldBeSameAs(ws2);
    }
}
