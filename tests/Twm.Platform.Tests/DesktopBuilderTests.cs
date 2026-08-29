using Twm.Core.Geometry;
using Twm.Core.Tree;
using Twm.Platform.Config;

namespace Twm.Platform.Tests;

public class DesktopBuilderTests
{
    private static MonitorInfo Mon(int x, int y, int w, int h, bool primary, int taskbar = 0) =>
        new(
            Id: new MonitorId(x + 1),
            Bounds: new Rect(x, y, w, h),
            WorkArea: new Rect(x, y, w, h - taskbar),
            IsPrimary: primary
        );

    private static IEnumerable<string> WorkspaceNames(Monitor monitor) =>
        monitor.Children.Cast<Workspace>().Select(workspace => workspace.Name);

    [Fact]
    public void TwoMonitors_ProduceInterleavedWorkspaces()
    {
        RootContainer root = DesktopBuilder.Build([
            Mon(0, 0, 1920, 1080, primary: true),
            Mon(1920, 0, 1280, 1024, primary: false),
        ]);

        root.Children.Count.ShouldBe(2);
        WorkspaceNames((Monitor)root.Children[0]).ShouldBe(["1", "3", "5", "7"]);
        WorkspaceNames((Monitor)root.Children[1]).ShouldBe(["2", "4", "6", "8"]);
    }

    [Fact]
    public void EachMonitorHasWorkspacesPerMonitorCount()
    {
        RootContainer root = DesktopBuilder.Build([
            Mon(0, 0, 1920, 1080, primary: true),
            Mon(1920, 0, 1280, 1024, primary: false),
        ]);

        foreach (Container child in root.Children)
        {
            ((Monitor)child).Children.Count.ShouldBe(DesktopBuilder.WorkspacesPerMonitor);
        }
    }

    [Fact]
    public void ThreeMonitors_InterleavedWorkspaces()
    {
        RootContainer root = DesktopBuilder.Build([
            Mon(0, 0, 1920, 1080, primary: true),
            Mon(1920, 0, 1280, 1024, primary: false),
            Mon(3200, 0, 1280, 1024, primary: false),
        ]);

        root.Children.Count.ShouldBe(3);
        WorkspaceNames((Monitor)root.Children[0]).ShouldBe(["1", "4", "7", "10"]);
        WorkspaceNames((Monitor)root.Children[1]).ShouldBe(["2", "5", "8", "11"]);
        WorkspaceNames((Monitor)root.Children[2]).ShouldBe(["3", "6", "9", "12"]);
    }

    [Fact]
    public void FirstWorkspaceOfEachMonitor_IsActive_PrimaryFocused()
    {
        RootContainer root = DesktopBuilder.Build([
            Mon(0, 0, 1920, 1080, primary: true),
            Mon(1920, 0, 1280, 1024, primary: false),
        ]);

        Monitor primary = (Monitor)root.Children[0];
        Monitor secondary = (Monitor)root.Children[1];
        primary.LastFocusedChild.ShouldBeSameAs(primary.Children[0]);
        secondary.LastFocusedChild.ShouldBeSameAs(secondary.Children[0]);
        root.LastFocusedChild.ShouldBeSameAs(primary);
    }

    [Fact]
    public void Workspaces_NumberedFromOne_PrimaryFirst()
    {
        RootContainer root = DesktopBuilder.Build([
            Mon(0, 0, 1920, 1080, primary: true),
            Mon(1920, 0, 1280, 1024, primary: false),
        ]);

        ((Workspace)((Monitor)root.Children[0]).Children[0]).Name.ShouldBe("1");
        ((Workspace)((Monitor)root.Children[1]).Children[0]).Name.ShouldBe("2");
    }

    [Fact]
    public void Monitor_UsesWorkAreaNotFullBounds()
    {
        int taskbar = 48;
        RootContainer root = DesktopBuilder.Build([
            Mon(0, 0, 1920, 1080, primary: true, taskbar: taskbar),
        ]);

        ((Monitor)root.Children[0]).Bounds.ShouldBe(new Rect(0, 0, 1920, 1080 - taskbar));
    }

    [Fact]
    public void PrimaryOrderedFirst_EvenWhenNotFirstInInput()
    {
        RootContainer root = DesktopBuilder.Build([
            Mon(0, 0, 1280, 1024, primary: false),
            Mon(1280, 0, 1920, 1080, primary: true),
        ]);

        Monitor first = (Monitor)root.Children[0];
        first.Bounds.X.ShouldBe(1280);
        ((Workspace)first.Children[0]).Name.ShouldBe("1");
    }

    [Fact]
    public void NonPrimaryMonitors_OrderedLeftToRight()
    {
        RootContainer root = DesktopBuilder.Build([
            Mon(2000, 0, 1000, 1000, primary: false), // right
            Mon(0, 0, 1000, 1000, primary: true), // primary
            Mon(1000, 0, 1000, 1000, primary: false), // middle
        ]);

        ((Monitor)root.Children[0]).Bounds.X.ShouldBe(0); // primary first
        // then left-to-right
        ((Monitor)root.Children[1]).Bounds.X.ShouldBe(1000);
        ((Monitor)root.Children[2]).Bounds.X.ShouldBe(2000);
    }

    [Fact]
    public void EmptyMonitorList_Throws()
    {
        Should.Throw<ArgumentException>(() => DesktopBuilder.Build([]));
    }

    [Fact]
    public void PerMonitorConfig_ChangesTheCount()
    {
        RootContainer root = DesktopBuilder.Build(
            [Mon(0, 0, 1920, 1080, primary: true), Mon(1920, 0, 1280, 1024, primary: false)],
            new WorkspacesDto { PerMonitor = 2 }
        );

        WorkspaceNames((Monitor)root.Children[0]).ShouldBe(["1", "3"]);
        WorkspaceNames((Monitor)root.Children[1]).ShouldBe(["2", "4"]);
    }

    [Fact]
    public void ExplicitNames_DistributedRoundRobin()
    {
        RootContainer root = DesktopBuilder.Build(
            [Mon(0, 0, 1920, 1080, primary: true), Mon(1920, 0, 1280, 1024, primary: false)],
            new WorkspacesDto { Names = ["a", "b", "c", "d", "e"] }
        );

        WorkspaceNames((Monitor)root.Children[0]).ShouldBe(["a", "c", "e"]);
        WorkspaceNames((Monitor)root.Children[1]).ShouldBe(["b", "d"]);
    }

    [Fact]
    public void ExplicitNames_FewerThanMonitors_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            DesktopBuilder.Build(
                [Mon(0, 0, 1920, 1080, primary: true), Mon(1920, 0, 1280, 1024, primary: false)],
                new WorkspacesDto { Names = ["only-one"] }
            )
        );
    }
}
