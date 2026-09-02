namespace Twm.Adapters.Config.Tests;

public class ConfigPathsTests
{
    [Fact]
    public void Default_EndsWithTwmConfigYaml()
    {
        string path = ConfigPaths.Default();

        path.ShouldEndWith(Path.Combine(".twm", "config.yaml"));
        Path.IsPathRooted(path).ShouldBeTrue();
    }
}
