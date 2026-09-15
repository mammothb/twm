namespace Twm.Adapters.Windows.Tests;

/// <summary>
/// Tests for <see cref="WinEventHook" />.
///
/// <para>
/// Event-delivery tests (verifying that <c>SetWinEventHook</c> actually
/// dispatches to the C# callback) are NOT included here. <c>WinEventHook</c>
/// registers with <c>WINEVENT_SKIPOWNPROCESS</c>, which means events for
/// windows in the test runner's own process are filtered out at the OS level.
/// A test that creates a window in-process will never see an event fire on
/// its own hook, regardless of how long it waits.
/// </para>
///
/// <para>
/// To test event delivery end-to-end, either: (a) add a way for
/// <c>WinEventHook.Install</c> to accept custom flags (skip
/// <c>SkipOwnProcess</c> for testing), or (b) spawn a child process that
/// creates a window in a different process. Both are deferred.
/// </para>
/// </summary>
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

        // After Dispose, the global state is cleared; a fresh hook should
        // install without throwing.
        using var second = new WinEventHook();
        second.Install(static (_, _) => { });
    }
}
