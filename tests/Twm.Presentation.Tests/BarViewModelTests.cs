using Twm.Application.Commands;
using Twm.Application.Coordination;
using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;
using Twm.TestSupport.Fakes;

namespace Twm.Presentation.Tests;

public class BarViewModelTests
{
    private static readonly DateTimeOffset s_now = new(2026, 9, 3, 22, 41, 30, TimeSpan.Zero);

    private static MonitorInfo Primary =>
        new(
            new MonitorId(1),
            new Rect(0, 0, 1920, 1080),
            new Rect(0, 0, 1920, 1080),
            IsPrimary: true
        );
    private static MonitorInfo Secondary =>
        new(
            new MonitorId(1),
            new Rect(1920, 0, 1280, 1024),
            new Rect(1920, 0, 1280, 1024),
            IsPrimary: false
        );

    private static NativeWindowInfo Win(int id, int x, int y) =>
        new(
            new WindowId(id),
            $"App{id}",
            "Notepad",
            new Rect(x, y, 800, 600),
            IsVisible: true,
            IsCloaked: false,
            IsToolWindow: false,
            IsMinimized: false
        );

    private static string TitleOf(WindowId id) => $"win{id.Value}";

    [Fact]
    public void Build_TwoMonitors_WorkspaceNamesActiveFlagsAndClock()
    {
        var session = new WmSession(
            new FakeMonitorSystem(Primary, Secondary),
            new FakeWindowSystem()
        );
        session.Start();

        BarSnapshot snapshot = BarViewModel.Build(session.Root, TitleOf, s_now);

        snapshot.Monitors.Count.ShouldBe(2);
        snapshot.Clock.ShouldBe("22:41");

        MonitorBarView primary = snapshot.Monitors[0];
        primary.Index.ShouldBe(0);
        primary.Workspaces.Select(w => w.Name).ShouldBe(["1", "3", "5", "7"]);
        primary.Workspaces[0].Active.ShouldBeTrue();
        primary.Workspaces.Count(w => w.Active).ShouldBe(1);

        MonitorBarView secondary = snapshot.Monitors[1];
        secondary.Index.ShouldBe(1);
        secondary.Workspaces.Select(w => w.Name).ShouldBe(["2", "4", "6", "8"]);
        secondary.Workspaces[0].Active.ShouldBeTrue();
    }

    [Fact]
    public void Build_OccupiedAndFocusedTitle_ReflectWindows()
    {
        var session = new WmSession(
            new FakeMonitorSystem(Primary, Secondary),
            new FakeWindowSystem(Win(1, 100, 100), Win(2, 200, 200), Win(3, 1920 + 100, 100))
        );
        session.Start();

        BarSnapshot snapshot = BarViewModel.Build(session.Root, TitleOf, s_now);

        MonitorBarView primary = snapshot.Monitors[0];
        primary.Workspaces.Single(w => w.Name == "1").Occupied.ShouldBeTrue();
        primary.Workspaces.Single(w => w.Name == "3").Occupied.ShouldBeFalse();
        primary.FocusedTitle.ShouldBe("win2");

        MonitorBarView secondary = snapshot.Monitors[1];
        secondary.Workspaces.Single(w => w.Name == "2").Occupied.ShouldBeTrue();
        secondary.FocusedTitle.ShouldBe("win3");
    }

    [Fact]
    public void Build_EmptyActiveWorkspace_HasNullTitle()
    {
        var session = new WmSession(
            new FakeMonitorSystem(Primary, Secondary),
            new FakeWindowSystem(Win(1, 100, 100))
        );
        session.Start();
        session.Execute(new FocusWorkspaceCommand("3"));

        BarSnapshot snapshot = BarViewModel.Build(session.Root, TitleOf, s_now);

        MonitorBarView primary = snapshot.Monitors[0];
        WorkspaceItem empty = primary.Workspaces.Single(w => w.Name == "3");
        empty.Active.ShouldBeTrue();
        empty.Occupied.ShouldBeFalse();
        primary.FocusedTitle.ShouldBeNull();
    }
}
