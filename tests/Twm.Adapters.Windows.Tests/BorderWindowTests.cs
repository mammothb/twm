namespace Twm.Adapters.Windows.Tests;

public sealed class BorderWindowTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void New_DoesNotThrow()
    {
        Should.NotThrow(() =>
        {
            using var window = new BorderWindow(color: 0x00FF0000u, width: 1);
        });
    }
}
