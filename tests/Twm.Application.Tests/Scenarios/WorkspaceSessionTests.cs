using Twm.Application.Commands;
using Twm.Application.Coordination;
using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;
using Twm.TestSupport.Fakes;

namespace Twm.Application.Tests.Scenarios;

public class WorkspaceSessionTests
{
    private static MonitorInfo Primary =>
        new(
            new MonitorId(1),
            new Rect(0, 0, 1920, 1080),
            new Rect(0, 0, 1920, 1080),
            IsPrimary: true
        );

    private static MonitorInfo Secondary =>
        new(
            new MonitorId(2),
            new Rect(1920, 0, 1280, 1024),
            new Rect(1920, 0, 1280, 1024),
            IsPrimary: false
        );

    private static NativeWindowInfo Win(int id, int x, int y) =>
        new(
            new WindowId(id),
            "App",
            "Notepad",
            new Rect(x, y, 800, 600),
            IsVisible: true,
            IsCloaked: false,
            IsToolWindow: false,
            IsMinimized: false
        );

    [Fact]
    public void FocusWorkspace_ActivatesTarget_CloaksPreviousActive()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100), Win(2, 200, 200));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start();
        session.Execute(new MoveWindowToWorkspaceCommand("3"));

        windows.Shown.Clear();
        windows.Hidden.Clear();
        session.Execute(new FocusWorkspaceCommand("3"));

        windows.Shown.ShouldContain(new WindowId(2));
        windows.Hidden.ShouldContain(new WindowId(1));
    }

    [Fact]
    public void SyncFocus_WindowOnInactiveWorkspace_ActivatesThatWorkspace()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100), Win(2, 200, 200));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start();
        session.Execute(new MoveWindowToWorkspaceCommand("3"));

        windows.Shown.Clear();
        windows.Hidden.Clear();
        session.SyncFocus(new WindowId(2));

        windows.Shown.ShouldContain(new WindowId(2));
        windows.Hidden.ShouldContain(new WindowId(1));
        session.Root.FocusedWindow()!.WindowId.ShouldBe(new WindowId(2));
    }

    [Fact]
    public void SyncFocus_WindowOnActiveWorkspace_UpdatesFocusWithoutReconcile()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100), Win(2, 200, 200));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start();

        windows.Positioned.Clear();
        windows.Shown.Clear();
        windows.Hidden.Clear();
        windows.Foregrounded.Clear();
        session.SyncFocus(new WindowId(1));

        session.Root.FocusedWindow()!.WindowId.ShouldBe(new WindowId(1));
        windows.Positioned.ShouldBeEmpty();
        windows.Hidden.ShouldBeEmpty();
        windows.Foregrounded.ShouldBeEmpty();
    }

    [Fact]
    public void FocusWorkspace_CrossMonitor_MovesFocusWithoutDisturbingOtherMonitor()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100), Win(2, 2000, 200));
        var session = new WmSession(new FakeMonitorSystem(Primary, Secondary), windows);
        session.Start();

        var secondary = (Monitor)session.Root.Children[1];
        Container secondaryActiveBefore = secondary.LastFocusedChild!;

        session.Execute(new FocusWorkspaceCommand("1"));

        session.Root.FocusedWindow()!.WindowId.ShouldBe(new WindowId(1));
        secondary.LastFocusedChild.ShouldBeSameAs(secondaryActiveBefore);
    }
}
