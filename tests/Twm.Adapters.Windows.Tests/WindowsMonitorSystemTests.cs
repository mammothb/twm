namespace Twm.Adapters.Windows.Tests;

public sealed class WindowsMonitorSystemTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void EnumerateMonitors_ReturnsAtLeastOne()
    {
        var ms = new WindowsMonitorSystem();

        ms.EnumerateMonitors().ShouldNotBeEmpty();
    }
}
