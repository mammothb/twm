using Twm.Core.Commands;
using Twm.Core.Geometry;
using Twm.Core.Tree;
using Twm.Platform.Tests.Fakes;

namespace Twm.Platform.Tests;

public class WmSessionTests
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

    private static NativeWindowInfo Win(
        int id,
        int x,
        int y,
        int w = 800,
        int h = 600,
        string cls = "Notepad",
        string title = "App"
    ) =>
        new(
            new WindowId(id),
            title,
            cls,
            new Rect(x, y, w, h),
            IsVisible: true,
            IsCloaked: false,
            IsToolWindow: false,
            IsMinimized: false
        );

    private static int WindowCount(WmSession session) =>
        session.Root.Descendants.OfType<TilingWindow>().Count();

    [Fact]
    public void Shutdown_ShowsEveryManagedWindow_IncludingOnesOnInactiveWorkspaces()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100), Win(2, 200, 200));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start();

        // move window 2 to an inactive workspace so it is cloaked
        session.SyncFocus(new WindowId(2));
        session.Execute(new MoveWindowToWorkspaceCommand("3"));

        windows.Shown.Clear();
        session.Shutdown();

        windows.Shown.ShouldContain(new WindowId(1));
        windows.Shown.ShouldContain(new WindowId(2));
        windows.Shown.Count.ShouldBe(2);
    }

    [Fact]
    public void Start_AdoptsManageableWindows_IgnoringUnmanageable()
    {
        var windows = new FakeWindowSystem(
            Win(1, 100, 100),
            Win(2, 200, 200),
            Win(3, 0, 0, cls: "Shell_TrayWnd", title: "") // taskbar -> ignored
        );
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);

        session.Start();

        WindowCount(session).ShouldBe(2);
        session.Root.FindWindow(new WindowId(1)).ShouldNotBeNull();
        session.Root.FindWindow(new WindowId(2)).ShouldNotBeNull();
        session.Root.FindWindow(new WindowId(3)).ShouldBeNull();
    }

    [Fact]
    public void Start_TilesWindowsOnTheirOwnMonitor_NoneCrossOver()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100), Win(2, 2000, 200));
        var session = new WmSession(new FakeMonitorSystem(Primary, Secondary), windows);

        session.Start();

        var primary = (Monitor)session.Root.Children[0];
        var secondary = (Monitor)session.Root.Children[1];
        TilingWindow w1 = session.Root.FindWindow(new WindowId(1))!;
        TilingWindow w2 = session.Root.FindWindow(new WindowId(2))!;
        w1.MonitorOf().ShouldBeSameAs(primary);
        w2.MonitorOf().ShouldBeSameAs(secondary);
        // each fills its own monitor's work area; nothing migrates across
        // displays
        w1.Bounds.ShouldBe(new Rect(0, 0, 1920, 1080));
        w2.Bounds.ShouldBe(new Rect(1920, 0, 1280, 1024));
    }

    [Fact]
    public void Start_TwoWindowsOnOneMonitor_SplitItExactly()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100), Win(2, 200, 200));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);

        session.Start();

        TilingWindow w1 = session.Root.FindWindow(new WindowId(1))!;
        TilingWindow w2 = session.Root.FindWindow(new WindowId(2))!;
        w1.Bounds.ShouldBe(new Rect(0, 0, 960, 1080));
        w2.Bounds.ShouldBe(new Rect(960, 0, 960, 1080));
        w2.Bounds.X.ShouldBe(w1.Bounds.Right);
    }

    [Fact]
    public void Start_PositionsEveryWindow_AndForegroundsTheFocusedOne()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100), Win(2, 200, 200));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);

        session.Start();

        windows.Positioned.Count.ShouldBe(2);
        windows.Positioned.ShouldContain(p => p.Window == new WindowId(1));
        windows.Positioned.ShouldContain(p => p.Window == new WindowId(2));
        windows.Foregrounded.ShouldBe([new WindowId(2)]);
    }

    [Fact]
    public void Start_NoWindows_PositionsNothing()
    {
        var windows = new FakeWindowSystem();
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);

        session.Start();

        windows.Positioned.ShouldBeEmpty();
        windows.Foregrounded.ShouldBeEmpty();
    }

    [Fact]
    public void Start_AdoptingWindows_EmitsLayoutChanged()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        int eventCount = 0;
        session.Subscribe<LayoutChangedEvent>(_ => eventCount++);

        session.Start();

        eventCount.ShouldBePositive();
    }

    [Fact]
    public void TryAdopt_UnmanageableWindow_ReturnsFalseAndPositionsNothing()
    {
        var windows = new FakeWindowSystem();
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start();

        bool adopted = session.TryAdopt(Win(9, 0, 0, cls: "Shell_TrayWnd", title: ""));

        adopted.ShouldBeFalse();
        WindowCount(session).ShouldBe(0);
        windows.Positioned.ShouldBeEmpty();
    }

    [Fact]
    public void TryAdopt_AlreadyManagedWindow_ReturnsFalseAndDoesNotDuplicate()
    {
        NativeWindowInfo win = Win(1, 100, 100);
        var windows = new FakeWindowSystem(win);
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start();

        bool adopted = session.TryAdopt(win);

        adopted.ShouldBeFalse();
        WindowCount(session).ShouldBe(1);
    }

    [Fact]
    public void Remove_UmanagedWindow_IsNoOp()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start();
        windows.Positioned.Clear(); // ignore startup positioning

        bool removed = session.Remove(new WindowId(999)); // never managed

        removed.ShouldBeFalse();
        WindowCount(session).ShouldBe(1);
        windows.Positioned.ShouldBeEmpty(); // no reconcile
    }

    [Fact]
    public void Remove_TakesWindowOutOfTree_AndRepositionsSurvivor()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100), Win(2, 200, 200));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start();

        session.Remove(new WindowId(1));

        session.Root.FindWindow(new WindowId(1)).ShouldBeNull();
        session.Root.FindWindow(new WindowId(2))!.Bounds.ShouldBe(new Rect(0, 0, 1920, 1080));
        windows.Positioned.ShouldContain((new WindowId(2), new Rect(0, 0, 1920, 1080)));
    }

    [Fact]
    public void HandleHidden_WindowOnInactiveWorkspace_IsIgnored()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start(); // win 1 adopted into active workspace "1"

        // Move to inactive workspace on the same monitor
        session.Execute(new MoveWindowToWorkspaceCommand("3"));

        // a hide event for an inactive workspace window is Twm's own cloak,
        // ignored
        session.HandleHidden(new WindowId(1)).ShouldBeFalse();
        session.IsManaged(new WindowId(1)).ShouldBeTrue();
    }

    [Fact]
    public void HandleHidden_WindowOnActiveWorkspace_IsRemoved()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start(); // win 1 on active workspace "1"

        // user hid/minimized it, drop it from tiling so no ghost tile is left
        session.HandleHidden(new WindowId(1)).ShouldBeTrue();
        session.IsManaged(new WindowId(1)).ShouldBeFalse();
    }

    [Fact]
    public void HandleHidden_NonFocusedTabIsIgnored_FocusedTabIsRemoved()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100), Win(2, 200, 200));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start(); // w1, w2 on active workspace "1", w2 is focused

        session.Execute(new SetLayoutCommand(LayoutMode.Tabbed));

        // w1 is non-focused tab (cloaked by Twm on the ACTIVE workspace), hide
        // is ignored
        session.HandleHidden(new WindowId(1)).ShouldBeFalse();
        session.IsManaged(new WindowId(1)).ShouldBeTrue();

        // w2 is the focused/visible tab, a hide of it is user-drive, removed
        session.HandleHidden(new WindowId(2)).ShouldBeTrue();
        session.IsManaged(new WindowId(2)).ShouldBeFalse();
    }

    [Fact]
    public void IsManaged_ReflectsWhetherWindowIsInTree()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start();

        session.IsManaged(new WindowId(1)).ShouldBeTrue();
        session.IsManaged(new WindowId(999)).ShouldBeFalse();
    }

    [Fact]
    public void SyncFocus_ManagedWindow_BecomesFocusSubject()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100), Win(2, 200, 200));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start(); // w2 is focused

        var windowId = new WindowId(1);
        session.SyncFocus(windowId);

        session.Root.FocusedWindow()!.WindowId.ShouldBe(windowId);
    }

    [Fact]
    public void SyncFocus_IgnoresTheForegroundEvenTwmItselfTriggered()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100), Win(2, 200, 200));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start(); // w2 is focused
        int eventCount = 0;
        session.Subscribe<LayoutChangedEvent>(_ => eventCount++);

        // The OS foreground event for the window Twm just foregrounded is
        // consumed, not reacted to (this is what stops a tabbed/stacked focus
        // change from oscillating)
        session.SyncFocus(new WindowId(2));

        eventCount.ShouldBe(0);
    }

    [Fact]
    public void SyncFocus_UnmanagedWindow_IsNoOp()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start();

        session.SyncFocus(new WindowId(999)); // never managed

        session.Root.FocusedWindow()!.WindowId.ShouldBe(new WindowId(1));
    }

    [Fact]
    public void SyncFocus_OnActiveWorkspaceWindow_EmitsLayoutChangedWithoutReconcile()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100), Win(2, 200, 200));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start();
        int eventCount = 0;
        session.Subscribe<LayoutChangedEvent>(_ => eventCount++);
        windows.Positioned.Clear();

        // both windows are on active workspace, this is a focus-only change
        session.SyncFocus(new WindowId(1));

        eventCount.ShouldBePositive();
        windows.Positioned.ShouldBeEmpty(); // focus only, no reconcile
    }

    [Fact]
    public void Execute_EmitsLayoutChanged()
    {
        var windows = new FakeWindowSystem(Win(1, 100, 100));
        var session = new WmSession(new FakeMonitorSystem(Primary), windows);
        session.Start();
        int eventCount = 0;
        session.Subscribe<LayoutChangedEvent>(_ => eventCount++);

        session.Execute(new FocusWorkspaceCommand("3"));

        eventCount.ShouldBePositive();
    }
}
