using Twm.Adapters.Windows;
using Twm.Application.Coordination;
using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;
using Twm.TestSupport.Fakes;

namespace Twm.App.Tests;

public sealed class WindowEventRouterTests
{
    [Fact]
    public void Handle_Appeared_NotManagedWindow_TriesAdopt()
    {
        var window = new NativeWindowInfo(
            Id: new WindowId(0xAAAA),
            Title: "new-window",
            ClassName: "TestClass",
            Bounds: new Rect(0, 0, 100, 100),
            IsVisible: true,
            IsCloaked: false,
            IsToolWindow: false,
            IsMinimized: false,
            IsChild: false,
            IsElevated: false,
            IsNoActivate: false,
            IsMenuPopup: false,
            IsLayered: false,
            HasCaption: true,
            HasWindowEdge: true,
            Owner: null,
            IsDlgModalFrame: false
        );
        (WmSession session, FakeMonitorSystem _, FakeWindowSystem windows) = BuildSession(window);
        WindowEventRouter router = BuildRouter(session, windows);

        router.Handle(WindowEventKind.Appeared, new WindowId(0xAAAA));

        session.IsManaged(new WindowId(0xAAAA)).ShouldBeTrue();
    }

    [Fact]
    public void Handle_Appeared_AlreadyManaged_SkipsAdopt()
    {
        var window = new NativeWindowInfo(
            Id: new WindowId(0xBBBB),
            Title: "twice",
            ClassName: "TestClass",
            Bounds: new Rect(0, 0, 100, 100),
            IsVisible: true,
            IsCloaked: false,
            IsToolWindow: false,
            IsMinimized: false,
            IsChild: false,
            IsElevated: false,
            IsNoActivate: false,
            IsMenuPopup: false,
            IsLayered: false,
            HasCaption: true,
            HasWindowEdge: true,
            Owner: null,
            IsDlgModalFrame: false
        );
        (WmSession session, FakeMonitorSystem _, FakeWindowSystem windows) = BuildSession(window);
        WindowEventRouter router = BuildRouter(session, windows);

        router.Handle(WindowEventKind.Appeared, new WindowId(0xBBBB));
        router.Handle(WindowEventKind.Appeared, new WindowId(0xBBBB));

        session.ManagedWindowCount.ShouldBe(1);
    }

    [Fact]
    public void Handle_Destroyed_ManagedWindow_Removes()
    {
        var window = new NativeWindowInfo(
            Id: new WindowId(0xCCCC),
            Title: "doomed",
            ClassName: "TestClass",
            Bounds: new Rect(0, 0, 100, 100),
            IsVisible: true,
            IsCloaked: false,
            IsToolWindow: false,
            IsMinimized: false,
            IsChild: false,
            IsElevated: false,
            IsNoActivate: false,
            IsMenuPopup: false,
            IsLayered: false,
            HasCaption: true,
            HasWindowEdge: true,
            Owner: null,
            IsDlgModalFrame: false
        );
        (WmSession session, FakeMonitorSystem _, FakeWindowSystem windows) = BuildSession(window);
        WindowEventRouter router = BuildRouter(session, windows);

        router.Handle(WindowEventKind.Appeared, new WindowId(0xCCCC));
        session.ManagedWindowCount.ShouldBe(1);

        router.Handle(WindowEventKind.Destroyed, new WindowId(0xCCCC));

        session.ManagedWindowCount.ShouldBe(0);
    }

    [Fact]
    public void Handle_Hidden_ManagedWindow_DelegatesToHandleHidden()
    {
        var window = new NativeWindowInfo(
            Id: new WindowId(0xDDDD),
            Title: "hidden-test",
            ClassName: "TestClass",
            Bounds: new Rect(0, 0, 100, 100),
            IsVisible: true,
            IsCloaked: false,
            IsToolWindow: false,
            IsMinimized: false,
            IsChild: false,
            IsElevated: false,
            IsNoActivate: false,
            IsMenuPopup: false,
            IsLayered: false,
            HasCaption: true,
            HasWindowEdge: true,
            Owner: null,
            IsDlgModalFrame: false
        );
        (WmSession session, FakeMonitorSystem _, FakeWindowSystem windows) = BuildSession(window);
        WindowEventRouter router = BuildRouter(session, windows);

        router.Handle(WindowEventKind.Appeared, new WindowId(0xDDDD));

        Should.NotThrow(() => router.Handle(WindowEventKind.Hidden, new WindowId(0xDDDD)));
    }

    [Fact]
    public void Handle_Minimized_ManagedWindow_DoesNotThrow()
    {
        var window = new NativeWindowInfo(
            Id: new WindowId(0xEEEE),
            Title: "min-test",
            ClassName: "TestClass",
            Bounds: new Rect(0, 0, 100, 100),
            IsVisible: true,
            IsCloaked: false,
            IsToolWindow: false,
            IsMinimized: false,
            IsChild: false,
            IsElevated: false,
            IsNoActivate: false,
            IsMenuPopup: false,
            IsLayered: false,
            HasCaption: true,
            HasWindowEdge: true,
            Owner: null,
            IsDlgModalFrame: false
        );
        (WmSession session, FakeMonitorSystem _, FakeWindowSystem windows) = BuildSession(window);
        WindowEventRouter router = BuildRouter(session, windows);

        router.Handle(WindowEventKind.Appeared, new WindowId(0xEEEE));

        Should.NotThrow(() => router.Handle(WindowEventKind.Minimized, new WindowId(0xEEEE)));
    }

    [Fact]
    public void Handle_Cloaked_UnknownWindow_DoesNotThrow()
    {
        (WmSession session, FakeMonitorSystem _, FakeWindowSystem windows) = BuildSession();
        WindowEventRouter router = BuildRouter(session, windows);

        Should.NotThrow(() => router.Handle(WindowEventKind.Cloaked, new WindowId(0x1111)));
    }

    [Fact]
    public void Handle_Foreground_UnknownWindow_DoesNotThrow()
    {
        (WmSession session, FakeMonitorSystem _, FakeWindowSystem windows) = BuildSession();
        WindowEventRouter router = BuildRouter(session, windows);

        Should.NotThrow(() => router.Handle(WindowEventKind.Foreground, new WindowId(0x2222)));
    }

    [Fact]
    public void Handle_MoveSizeEnd_ManagedWindow_RoutesToSessionAndApplies()
    {
        // Route the event through the router, not directly to the session:
        // proves the router actually calls HandleMoveSizeEnd (an empty session
        // would also pass the "does not throw" check on its own).
        var window = new NativeWindowInfo(
            Id: new WindowId(0x3333),
            Title: "snapped",
            ClassName: "TestClass",
            Bounds: new Rect(0, 0, 100, 100),
            IsVisible: true,
            IsCloaked: false,
            IsToolWindow: false,
            IsMinimized: false,
            IsChild: false,
            IsElevated: false,
            IsNoActivate: false,
            IsMenuPopup: false,
            IsLayered: false,
            HasCaption: true,
            HasWindowEdge: true,
            Owner: null,
            IsDlgModalFrame: false
        );
        (WmSession session, FakeMonitorSystem _, FakeWindowSystem windows) = BuildSession(window);
        WindowEventRouter router = BuildRouter(session, windows);
        router.Handle(WindowEventKind.Appeared, window.Id); // adopt
        windows.Positioned.Clear(); // ignore startup positioning

        router.Handle(WindowEventKind.MoveSizeEnd, window.Id);

        windows.Positioned.ShouldContain((window.Id, new Rect(0, 0, 1920, 1080)));
    }

    [Fact]
    public void Handle_MoveSizeEnd_UnknownWindow_DoesNotThrow()
    {
        (WmSession session, FakeMonitorSystem _, FakeWindowSystem windows) = BuildSession();
        WindowEventRouter router = BuildRouter(session, windows);

        Should.NotThrow(() => router.Handle(WindowEventKind.MoveSizeEnd, new WindowId(0x3333)));
    }

    private static (
        WmSession session,
        FakeMonitorSystem monitors,
        FakeWindowSystem windows
    ) BuildSession(params NativeWindowInfo[] initialWindows)
    {
        var monitors = new FakeMonitorSystem(
            new MonitorInfo(
                new MonitorId(1),
                new Rect(0, 0, 1920, 1080),
                new Rect(0, 0, 1920, 1080),
                IsPrimary: true
            )
        );
        var windows = new FakeWindowSystem(initialWindows);
        var session = new WmSession(monitors, windows);
        return (session, monitors, windows);
    }

    private static WindowEventRouter BuildRouter(WmSession session, FakeWindowSystem windows)
    {
        // We don't call Install (it touches OS hooks); the router is built and
        // Handle is invoked directly via its internal surface (InternalsVisibleTo).
        return new WindowEventRouter(session, windows);
    }
}
