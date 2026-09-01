using Twm.Application.Coordination;
using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.TestSupport.Fakes;

namespace Twm.Application.Tests.Coordination;

public class InsetMonitorSystemTests
{
    private static MonitorInfo Monitor(int taskbar = 0) =>
        new(
            new MonitorId(1),
            new Rect(0, 0, 1920, 1080),
            new Rect(0, 0, 1920, 1080 - taskbar),
            IsPrimary: true
        );

    [Fact]
    public void Top_ShrinksWorkAreaFromTheTop_BoundsUntouched()
    {
        var inset = new InsetMonitorSystem(new FakeMonitorSystem(Monitor()), 28, BarPosition.Top);

        MonitorInfo result = inset.EnumerateMonitors()[0];

        result.WorkArea.ShouldBe(new Rect(0, 28, 1920, 1080 - 28));
        result.Bounds.ShouldBe(new Rect(0, 0, 1920, 1080));
    }

    [Fact]
    public void Bottom_ShrinksWorkAreaFromTheBottom()
    {
        var inset = new InsetMonitorSystem(
            new FakeMonitorSystem(Monitor()),
            28,
            BarPosition.Bottom
        );

        MonitorInfo result = inset.EnumerateMonitors()[0];

        result.WorkArea.ShouldBe(new Rect(0, 0, 1920, 1080 - 28));
    }

    [Fact]
    public void Inset_StacksOnTopOfAnExistingTaskbarWorkArea()
    {
        var inset = new InsetMonitorSystem(new FakeMonitorSystem(Monitor(48)), 28, BarPosition.Top);

        MonitorInfo result = inset.EnumerateMonitors()[0];

        result.WorkArea.ShouldBe(new Rect(0, 28, 1920, 1080 - 48 - 28));
    }

    [Fact]
    public void BarTallerThanMonitor_DoesNotProduceNegativeHeight()
    {
        var inset = new InsetMonitorSystem(new FakeMonitorSystem(Monitor()), 5000, BarPosition.Top);

        MonitorInfo result = inset.EnumerateMonitors()[0];

        result.WorkArea.ShouldBe(new Rect(0, 1080, 1920, 0));
    }
}
