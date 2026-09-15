namespace Twm.Adapters.Windows.Tests;

public sealed class TabBarWindowTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void New_DoesNotThrow()
    {
        Should.NotThrow(() =>
        {
            using var window = new TabBarWindow(
                background: 0x00303030u,
                foreground: 0x00E0E0E0u,
                accent: 0x00775528u,
                rowHeight: 28
            );
        });
    }
}
