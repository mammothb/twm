using Twm.Application.Config;
using Twm.Domain.Geometry;

namespace Twm.Adapters.Windows.Tests;

public sealed class StatusBarWindowTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void New_DoesNotThrow()
    {
        Should.NotThrow(() =>
        {
            using var window = new StatusBarWindow(
                bounds: new Rect(0, 0, 100, 28),
                options: BarOptions.Defaults
            );
        });
    }
}
