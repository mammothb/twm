using Twm.Core.Commands;
using Twm.Core.Geometry;
using Twm.Core.Tree;

namespace Twm.Core.Tests.Scenarios;

public class DualMonitorSessionTests
{
    [Fact]
    public void EachMonitorTilesIndependently()
    {
        var world = new TestWorld();

        world.Adopt(1, world.Primary);
        world.Adopt(2, world.Primary);
        world.Adopt(3, world.Secondary);
        world.Adopt(4, world.Secondary);

        string expected =
            "Root\n"
            + "  Monitor [0,0 1920x1080]\n"
            + "    Workspace \"1\" Horizontal [0,0 1920x1080]\n"
            + "      Window #1 [0,0 960x1080]\n"
            + "      Window #2 [960,0 960x1080]\n"
            + "  Monitor [1920,0 1280x1024]\n"
            + "    Workspace \"2\" Horizontal [1920,0 1280x1024]\n"
            + "      Window #3 [1920,0 640x1024]\n"
            + "      Window #4 [2560,0 640x1024]\n";

        TreeRenderer.Render(world.Root).ShouldBe(expected);
    }

    [Fact]
    public void AdoptingOnSecondaryDoesNotMovePrimaryWindows()
    {
        var world = new TestWorld();

        world.Adopt(1, world.Primary);
        Rect before = world.Window(1).Bounds;

        world.Adopt(2, world.Secondary);
        world.Adopt(3, world.Secondary);

        before.ShouldBe(new Rect(0, 0, 1920, 1080));
        world.Window(1).Bounds.ShouldBe(before);
        world.Window(2).MonitorOf().ShouldBeSameAs(world.Secondary);
        world.Window(3).MonitorOf().ShouldBeSameAs(world.Secondary);
    }

    [Fact]
    public void MovingToAWorkspaceOnAnotherMonitorRelocatesTheWindow()
    {
        var world = new TestWorld();

        world.Adopt(1, world.Primary);
        world.Adopt(2, world.Primary);
        world.Invoke(new MoveWindowToWorkspaceCommand("2"));

        Workspace primaryWs = TestWorld.WorkspaceOf(world.Primary, 0);
        Workspace secondaryWs = TestWorld.WorkspaceOf(world.Secondary, 0);
        primaryWs.Children.ShouldBe([world.Window(1)]);
        secondaryWs.Children.ShouldBe([world.Window(2)]);
        world.Window(2).MonitorOf().ShouldBeSameAs(world.Secondary);
        world.Window(1).Bounds.ShouldBe(new Rect(0, 0, 1920, 1080));
        world.Window(2).Bounds.ShouldBe(new Rect(1920, 0, 1280, 1024));
    }

    [Fact]
    public void FocusRight_AtPrimaryEdge_CrossesToSecondary()
    {
        var world = new TestWorld();
        world.Adopt(1, world.Primary);
        world.Adopt(2, world.Secondary);
        world.Window(1).Focus();

        world.Invoke(new FocusInDirectionCommand(Direction.Right));

        world.Root.FocusedWindow()!.WindowId.ShouldBe(new WindowId(2));
    }

    [Fact]
    public void FocusRight_AtPrimaryEdge_LastOnSecondarysEdgeWindowInsteadOfLastFocused()
    {
        var world = new TestWorld();
        world.Adopt(1, world.Primary);
        world.Adopt(2, world.Secondary); // left on secondary
        world.Adopt(3, world.Secondary); // right on secondary
        world.Window(3).Focus(); // focus right on secondary
        world.Window(1).Focus();

        world.Invoke(new FocusInDirectionCommand(Direction.Right));

        // Must land on secondary's leftmost window (2) and not last focused (3)
        world.Root.FocusedWindow()!.WindowId.ShouldBe(new WindowId(2));
    }

    [Fact]
    public void MoveRight_AtPrimaryEdge_RelocatesToSecondaryAndFollowsFocus()
    {
        var world = new TestWorld();
        world.Adopt(1, world.Primary);
        world.Adopt(2, world.Secondary);
        world.Window(1).Focus();

        world.Invoke(new MoveInDirectionCommand(Direction.Right));

        world.Window(1).MonitorOf().ShouldBeSameAs(world.Secondary);
        world.Root.FocusedWindow()!.WindowId.ShouldBe(new WindowId(1));
    }
}
