using Twm.Application.Config;

namespace Twm.Adapters.Config.Tests;

public sealed class YamlConfigSourceTests : IDisposable
{
    private const string ValidYaml = """
        mod: alt
        gaps:
            inner: 8
            outer: 12
        """;

    private const string MalformedYaml = "gaps: {inner: 8, outer: 12";

    private readonly string _path = Path.Combine(
        Path.GetTempPath(),
        $"twm-test-{Guid.NewGuid():N}.yaml"
    );

    public void Dispose() => File.Delete(_path);

    [Fact]
    public void Load_FileDoesNotExist_ReturnsResolvedConfigWithDefaults()
    {
        var source = new YamlConfigSource(_path);

        ResolvedConfig resolved = source.Load(monitorCount: 1);

        resolved.Errors.ShouldBeEmpty();
        resolved.Gaps.Inner.ShouldBe(0);
        resolved.Gaps.Outer.ShouldBe(0);
    }

    [Fact]
    public void Load_ValidYamlFile_ReturnsResolvedConfig()
    {
        File.WriteAllText(_path, ValidYaml);
        var source = new YamlConfigSource(_path);

        ResolvedConfig resolved = source.Load(monitorCount: 1);

        resolved.Errors.ShouldBeEmpty();
        resolved.Gaps.Inner.ShouldBe(8);
        resolved.Gaps.Outer.ShouldBe(12);
    }

    [Fact]
    public void Load_MalformedYamlFile_PrependsParseErrorToErrors()
    {
        File.WriteAllText(_path, MalformedYaml);
        var source = new YamlConfigSource(_path);

        ResolvedConfig resolved = source.Load(monitorCount: 1);

        // The VYaml parse error is appended to whatever the resolver produced
        // on the fallback defaults (empty here, but the path is exercised)
        resolved.Errors.ShouldNotBeEmpty();
        resolved.Gaps.Inner.ShouldBe(0);
    }
}
