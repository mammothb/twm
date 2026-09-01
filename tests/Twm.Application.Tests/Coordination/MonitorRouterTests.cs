using Twm.Application.Coordination;
using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Application.Tests.Coordination;

public class MonitorRouterTests
{
    private static RootContainer TwoMonitors() =>
        DesktopBuilder.Build([
            new MonitorInfo(
                new MonitorId(1),
                new Rect(0, 0, 1920, 1080),
                new Rect(0, 0, 1920, 1080),
                IsPrimary: true
            ),
            new MonitorInfo(
                new MonitorId(2),
                new Rect(0, 0, 1280, 1024),
                new Rect(1920, 0, 1280, 1024),
                IsPrimary: false
            ),
        ]);

    [Fact]
    public void WindowCenterInPrimary_PicksPrimary()
    {
        RootContainer root = TwoMonitors();
        Monitor primary = (Monitor)root.Children[0];

        MonitorRouter.Pick(root, new Rect(100, 100, 800, 600)).ShouldBeSameAs(primary);
    }

    [Fact]
    public void WindowCenterInSecondary_PicksSecondary()
    {
        RootContainer root = TwoMonitors();
        Monitor secondary = (Monitor)root.Children[1];

        MonitorRouter.Pick(root, new Rect(2000, 100, 800, 600)).ShouldBeSameAs(secondary);
    }

    [Theory]
    [InlineData(-2000)]
    [InlineData(5000)]
    public void OffscreenWindow_FallsBackToPrimary(int x)
    {
        RootContainer root = TwoMonitors();
        Monitor primary = (Monitor)root.Children[0];

        MonitorRouter.Pick(root, new Rect(x, 100, 800, 600)).ShouldBeSameAs(primary);
    }

    [Fact]
    public void WindowInGapBelowShorterMonitor_FallsBackToPrimary()
    {
        RootContainer root = TwoMonitors();
        Monitor primary = (Monitor)root.Children[0];

        // x in secondary, but y=1040 is out of 1024 range of secondary
        MonitorRouter.Pick(root, new Rect(2400, 1040, 800, 600)).ShouldBeSameAs(primary);
    }
}
