using Twm.Adapters.Config;

namespace Twm.App.Tests;

public sealed class CliArgsTests
{
    [Fact]
    public void Parse_EmptyArgs_AllFlagsFalseAndConfigPathDefault()
    {
        CliArgs args = CliArgs.Parse([]);

        args.Dump.ShouldBeFalse();
        args.UseConsole.ShouldBeFalse();
        args.Log.ShouldBeFalse();
        args.ConfigPath.ShouldBe(ConfigPaths.Default());
    }

    [Fact]
    public void Parse_DumpFlag_SetsOnlyDumpTrue()
    {
        CliArgs args = CliArgs.Parse(["--dump"]);

        args.Dump.ShouldBeTrue();
        args.UseConsole.ShouldBeFalse();
        args.Log.ShouldBeFalse();
    }

    [Fact]
    public void Parse_AllThreeBooleanFlags_SetsAllTrue()
    {
        CliArgs args = CliArgs.Parse(["--dump", "--console", "--log"]);

        args.Dump.ShouldBeTrue();
        args.UseConsole.ShouldBeTrue();
        args.Log.ShouldBeTrue();
    }

    [Fact]
    public void Parse_ConfigFlagWithValue_UsesProvidedPath()
    {
        CliArgs args = CliArgs.Parse(["--config", "/some/path/to/config.yaml"]);

        args.ConfigPath.ShouldBe("/some/path/to/config.yaml");
    }

    [Fact]
    public void Parse_ConfigFlagAtEndWithoutValue_FallsBackToDefault()
    {
        // --config is the last token; nothing after it to consume as the
        // path. Behavior: fall back to default rather than throwing.
        CliArgs args = CliArgs.Parse(["--dump", "--config"]);

        args.Dump.ShouldBeTrue();
        args.ConfigPath.ShouldBe(ConfigPaths.Default());
    }

    [Fact]
    public void Parse_MixedFlagsAndConfig_ParsesEachIndependently()
    {
        CliArgs args = CliArgs.Parse(["--dump", "--config", "/x/y.yaml", "--log"]);

        args.Dump.ShouldBeTrue();
        args.Log.ShouldBeTrue();
        args.UseConsole.ShouldBeFalse();
        args.ConfigPath.ShouldBe("/x/y.yaml");
    }
}
