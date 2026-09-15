namespace Twm.Adapters.Windows.Tests;

public sealed class WinEventHookTests
{
    public static bool IsWindows => OperatingSystem.IsWindows();

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Install_Twice_Throws()
    {
        using var first = new WinEventHook();
        first.Install(static (_, _) => { });

        using var second = new WinEventHook();
        Should.Throw<InvalidOperationException>(() => second.Install(static (_, _) => { }));
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void Dispose_AllowsReinstall()
    {
        using var first = new WinEventHook();
        first.Install(static (_, _) => { });

        first.Dispose();

        using var second = new WinEventHook();
        second.Install(static (_, _) => { });
    }
}
