using Twm.Adapters.Config;

namespace Twm.App;

/// <summary>
/// Parsed <c>twm</c> command-line flags. See <see cref="Parse" /> for the
/// supported flags. <c>Program.cs</c> reads only this record and never
/// matches against <c>args</c> directly.
/// </summary>
internal sealed record CliArgs(bool Dump, bool UseConsole, bool Log, string ConfigPath)
{
    /// <summary>
    /// Reads the supported flags from <paramref name="args" />. Each flag is
    /// optional. <c>--config</c> is followed by its path; falls back to
    /// <see cref="ConfigPaths.Default" /> when absent.
    /// </summary>
    public static CliArgs Parse(string[] args) =>
        new(
            Dump: args.Contains("--dump"),
            UseConsole: args.Contains("--console"),
            Log: args.Contains("--log"),
            ConfigPath: ParseConfigPath(args)
        );

    private static string ParseConfigPath(string[] args)
    {
        int configIndex = Array.IndexOf(args, "--config");
        return 0 <= configIndex && configIndex < args.Length - 1
            ? args[configIndex + 1]
            : ConfigPaths.Default();
    }
}
