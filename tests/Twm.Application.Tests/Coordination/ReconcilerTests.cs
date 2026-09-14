using Twm.Application.Coordination;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;
using Twm.TestSupport.Fakes;

namespace Twm.Application.Tests.Coordination;

public class ReconcilerTests
{
    [Fact]
    public void Apply_ShowsAndPositionsActiveWorkspace_HidesInactive()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 1920, 1080));
        var active = new Workspace("1");
        var inactive = new Workspace("2");
        monitor.AppendChild(active);
        monitor.AppendChild(inactive);
        root.AppendChild(monitor);

        var activeWindow = new TilingWindow(new WindowId(1));
        var inactiveWindow = new TilingWindow(new WindowId(2));
        active.AppendChild(activeWindow);
        inactive.AppendChild(inactiveWindow);

        active.Focus();

        new LayoutEngine().Arrange(root);
        var windows = new FakeWindowSystem();
        new Reconciler(windows).Apply(root);

        windows.Positioned.ShouldContain(entry => entry.Window == new WindowId(1));
        windows.Shown.ShouldContain(new WindowId(1));
        windows.Hidden.ShouldContain(new WindowId(2));
        windows.Positioned.ShouldNotContain(entry => entry.Window == new WindowId(2));
        windows.Foregrounded.ShouldBe([new WindowId(1)]);
    }

    [Fact]
    public void Apply_WhenOneWindowThrows_StillPositionsTheOthers()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 1920, 1080));
        var ws = new Workspace("1");
        monitor.AppendChild(ws);
        root.AppendChild(monitor);

        var bad = new TilingWindow(new WindowId(1));
        var good = new TilingWindow(new WindowId(2));
        ws.AppendChild(bad);
        ws.AppendChild(good);
        good.Focus();

        new LayoutEngine().Arrange(root);
        var windows = new FakeWindowSystem();
        windows.ThrowOnRect.Add(new WindowId(1));

        new Reconciler(windows).Apply(root);

        windows.Positioned.ShouldNotContain(entry => entry.Window == new WindowId(1));
        windows.Positioned.ShouldContain(entry => entry.Window == new WindowId(2));
    }

    [Fact]
    public void Apply_ForegroundsFocusedBeforeCloakingOthers()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 1920, 1080));
        var ws = new Workspace("1", Layout.Tabbed);
        monitor.AppendChild(ws);
        root.AppendChild(monitor);
        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        ws.AppendChild(w1);
        ws.AppendChild(w2);
        w1.Focus();
        new LayoutEngine().Arrange(root);

        var windows = new FakeWindowSystem();
        new Reconciler(windows).Apply(root);

        int foreground = windows.Operations.IndexOf("foreground:1");
        int hide = windows.Operations.IndexOf("hide:2");
        foreground.ShouldBePositive();
        hide.ShouldBePositive();
        foreground.ShouldBeLessThan(
            hide,
            "focused window must be foregrounded before others are cloaked"
        );
    }

    [Fact]
    public void TabbedWorkspace_ShowsOnlyTheFocusedTab_AndSwitchingFlipsIt()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 1920, 1080));
        var ws = new Workspace("1", Layout.Tabbed);
        monitor.AppendChild(ws);
        root.AppendChild(monitor);

        var w1 = new TilingWindow(new WindowId(1));
        var w2 = new TilingWindow(new WindowId(2));
        var w3 = new TilingWindow(new WindowId(3));
        ws.AppendChild(w1);
        ws.AppendChild(w2);
        ws.AppendChild(w3);

        w1.Focus();
        new LayoutEngine().Arrange(root);
        var first = new FakeWindowSystem();
        new Reconciler(first).Apply(root);

        first.Positioned.ShouldContain(entry => entry.Window == new WindowId(1));
        first.Shown.ShouldContain(new WindowId(1));
        first.Hidden.ShouldContain(new WindowId(2));
        first.Hidden.ShouldContain(new WindowId(3));
        first.Positioned.ShouldNotContain(entry => entry.Window == new WindowId(2));

        w2.Focus();
        new LayoutEngine().Arrange(root);
        var second = new FakeWindowSystem();
        new Reconciler(second).Apply(root);

        second.Positioned.ShouldContain(entry => entry.Window == new WindowId(2));
        second.Shown.ShouldContain(new WindowId(2));
        second.Hidden.ShouldContain(new WindowId(1));
    }

    [Fact]
    public void TabbedWorkspace_DoesNotCloakTheOwnerOfTheFocusedDialog()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 1920, 1080));
        var ws = new Workspace("1", Layout.Tabbed);
        monitor.AppendChild(ws);
        root.AppendChild(monitor);

        var owner = new TilingWindow(new WindowId(1));
        var dialog = new TilingWindow(new WindowId(2), owner: new WindowId(1));
        var other = new TilingWindow(new WindowId(3));
        ws.AppendChild(owner);
        ws.AppendChild(dialog);
        ws.AppendChild(other);
        dialog.Focus();
        new LayoutEngine().Arrange(root);

        var windows = new FakeWindowSystem();
        new Reconciler(windows).Apply(root);

        // the owner of the focused dialog must NOT be cloaked (cloak cascades
        // owner→owned and would hide the dialog); an unrelated non-focused tab
        // still is
        windows.Shown.ShouldContain(new WindowId(2));
        windows.Hidden.ShouldNotContain(new WindowId(1));
        windows.Hidden.ShouldContain(new WindowId(3));
    }

    [Fact]
    public void TabbedWorkspace_UncloaksTheOwnerWhenItsDialogBecomesFocused()
    {
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 1920, 1080));
        var ws = new Workspace("1", Layout.Tabbed);
        monitor.AppendChild(ws);
        root.AppendChild(monitor);

        var owner = new TilingWindow(new WindowId(1));
        var dialog = new TilingWindow(new WindowId(2), owner: new WindowId(1));
        var other = new TilingWindow(new WindowId(3));
        ws.AppendChild(owner);
        ws.AppendChild(dialog);
        ws.AppendChild(other);

        // Pass 1: focus the unrelated window — the owner becomes a non-focused
        // tab and is cloaked (it owns no visible window at this point)
        other.Focus();
        new LayoutEngine().Arrange(root);
        var first = new FakeWindowSystem();
        new Reconciler(first).Apply(root);
        first.Hidden.ShouldContain(new WindowId(1));

        // Pass 2: focus the dialog — the owner must be uncloaked (shown), not
        // just left un-hidden, so DWM doesn't keep the dialog hidden through
        // the owner relationship
        dialog.Focus();
        new LayoutEngine().Arrange(root);
        var second = new FakeWindowSystem();
        new Reconciler(second).Apply(root);
        second.Shown.ShouldContain(new WindowId(2));
        second.Shown.ShouldContain(new WindowId(1));
    }

    [Fact]
    public void Ctor_NullWindowSystem_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new Reconciler(null!));
    }

    [Fact]
    public void Apply_NullRoot_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new Reconciler(new FakeWindowSystem()).Apply(null!)
        );
    }

    [Fact]
    public void Apply_WithEmptyTree_PerformsNoWindowOperations()
    {
        // no monitors, no workspaces, no windows -> every loop body is empty
        // and the FocusedWindow branch is skipped
        var root = new RootContainer();
        var windows = new FakeWindowSystem();

        new Reconciler(windows).Apply(root);

        windows.Operations.ShouldBeEmpty();
        windows.Positioned.ShouldBeEmpty();
        windows.Shown.ShouldBeEmpty();
        windows.Hidden.ShouldBeEmpty();
        windows.Foregrounded.ShouldBeEmpty();
    }

    [Fact]
    public void Apply_WithoutFocusedWindow_DoesNotCallSetForeground()
    {
        // tree has a monitor and workspace but no tiling window -> FocusedWindow
        // is null, so the SetForeground branch must be skipped (not entered and
        // not swallowed)
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 1920, 1080));
        monitor.AppendChild(new Workspace("1"));
        root.AppendChild(monitor);
        new LayoutEngine().Arrange(root);

        var windows = new FakeWindowSystem();

        new Reconciler(windows).Apply(root);

        windows.Foregrounded.ShouldBeEmpty();
    }

    [Fact]
    public void Apply_WhenSetForegroundThrows_ContinuesAndStillCloaksOthers()
    {
        // the foreground call can race the window's destruction; the
        // reconciler must swallow it and still cloak the rest in pass 2
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 1920, 1080));
        var active = new Workspace("1");
        var inactive = new Workspace("2");
        monitor.AppendChild(active);
        monitor.AppendChild(inactive);
        root.AppendChild(monitor);

        var focused = new TilingWindow(new WindowId(1));
        var inactiveWindow = new TilingWindow(new WindowId(2));
        active.AppendChild(focused);
        inactive.AppendChild(inactiveWindow);
        focused.Focus();
        new LayoutEngine().Arrange(root);

        var windows = new FakeWindowSystem();
        windows.ThrowOnForeground.Add(new WindowId(1));

        Should.NotThrow(() => new Reconciler(windows).Apply(root));
        // pass 2 ran despite the foreground failure
        windows.Hidden.ShouldContain(new WindowId(2));
    }

    [Fact]
    public void Apply_WhenHideThrows_StillHidesTheOthers()
    {
        // one stubborn window refuses to be cloaked; the rest still must be
        var root = new RootContainer();
        var monitor = new Monitor(new Rect(0, 0, 1920, 1080));
        var active = new Workspace("1");
        var inactive = new Workspace("2");
        monitor.AppendChild(active);
        monitor.AppendChild(inactive);
        root.AppendChild(monitor);

        var activeWindow = new TilingWindow(new WindowId(1));
        var stubborn = new TilingWindow(new WindowId(2));
        var other = new TilingWindow(new WindowId(3));
        active.AppendChild(activeWindow);
        inactive.AppendChild(stubborn);
        inactive.AppendChild(other);
        activeWindow.Focus();
        new LayoutEngine().Arrange(root);

        var windows = new FakeWindowSystem();
        windows.ThrowOnHide.Add(new WindowId(2));

        Should.NotThrow(() => new Reconciler(windows).Apply(root));
        // the stubborn window was skipped, the other inactive window was
        // hidden
        windows.Hidden.ShouldContain(new WindowId(3));
        windows.Hidden.ShouldNotContain(new WindowId(2));
    }
}
