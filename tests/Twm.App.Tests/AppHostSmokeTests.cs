namespace Twm.App.Tests;

public sealed class AppHostSmokeTests : IDisposable
{
    private readonly string _tempConfigPath = Path.Combine(
        Path.GetTempPath(),
        $"twm-smoke-{Guid.NewGuid():N}.yaml"
    );

    public static bool IsWindows => OperatingSystem.IsWindows();

    public void Dispose()
    {
        if (File.Exists(_tempConfigPath))
        {
            File.Delete(_tempConfigPath);
        }
    }

    [Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
    public void BuildThenDispose_ConstructsAndTearsDownCleanly()
    {
        // Empty config → ConfigLoader returns TwmConfig.Defaults; no parse
        // errors. Build shouldn't throw for a valid (default) config.
        File.WriteAllText(_tempConfigPath, string.Empty);

        CliArgs args = new(Dump: false, UseConsole: false, Log: false, ConfigPath: _tempConfigPath);

        Should.NotThrow(() =>
        {
            using var host = AppHost.Build(args);

            // Run() is intentionally not called — it blocks on the message loop.
            // Dispose cascades teardown via `using` when the lambda exits.
            // If any subsystem leaks (handle not closed, double-dispose, etc.),
            // Dispose throws and Should.NotThrow fails the test.
        });
    }
}
