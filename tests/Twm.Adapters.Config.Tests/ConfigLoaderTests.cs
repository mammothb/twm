namespace Twm.Adapters.Config.Tests;

public class ConfigLoaderTests
{
    private const string FullYaml = """
        mod: alt
        gaps:
            inner: 8
            outer: 12
        workspaces:
            perMonitor: 4
        bindings:
            "$mod+h": focus left
            "$mod+shift+q": close
        windowRules:
            - class: TaskManagerWindow
              action: ignore
            - title: Picture in picture
              action: ignore
        """;

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("\n\t")]
    public void Load_EmptyOrWhitespace_ReturnsDefaultsNoErrors(string yaml)
    {
        ConfigLoadResult result = ConfigLoader.Load(yaml);

        result.Errors.ShouldBeEmpty();
        result.Config.Mod.ShouldBe("alt");
        result.Config.Gaps!.Inner.ShouldBe(0);
        result.Config.Gaps!.Outer.ShouldBe(0);
        result.Config.Workspaces!.PerMonitor.ShouldBe(4);
        result.Config.Bindings.ShouldBeNull();
        result.Config.WindowRules.ShouldBeNull();
    }

    [Fact]
    public void Load_FullValidYaml_PopulatesEverySection()
    {
        ConfigLoadResult result = ConfigLoader.Load(FullYaml);

        result.Errors.ShouldBeEmpty();
        result.Config.Mod.ShouldBe("alt");
        result.Config.Gaps!.Inner.ShouldBe(8);
        result.Config.Gaps!.Outer.ShouldBe(12);
        result.Config.Workspaces!.PerMonitor.ShouldBe(4);

        result.Config.Bindings!.Count.ShouldBe(2);
        result.Config.Bindings["$mod+h"].ShouldBe("focus left");
        result.Config.Bindings["$mod+shift+q"].ShouldBe("close");

        result.Config.WindowRules!.Count.ShouldBe(2);
        result.Config.WindowRules[0].Class.ShouldBe("TaskManagerWindow");
        result.Config.WindowRules[0].Action.ShouldBe("ignore");
        result.Config.WindowRules[1].Title.ShouldBe("Picture in picture");
        result.Config.WindowRules[1].Action.ShouldBe("ignore");
    }

    [Fact]
    public void Load_PartialYaml_LeavesAbsentSectionsNull()
    {
        ConfigLoadResult result = ConfigLoader.Load("mod: win\n");

        result.Errors.ShouldBeEmpty();
        result.Config.Mod.ShouldBe("win");
        result.Config.Gaps.ShouldBeNull();
        result.Config.Workspaces.ShouldBeNull();
        result.Config.Bindings.ShouldBeNull();
        result.Config.WindowRules.ShouldBeNull();
    }

    [Fact]
    public void Load_MalformedYaml_ReturnsDefaultsWithError()
    {
        ConfigLoadResult result = ConfigLoader.Load("gaps: {inner: 8, outer: 12");

        result.Errors.ShouldNotBeEmpty();
        result.Config.Mod.ShouldBe("alt");
        result.Config.Gaps!.Inner.ShouldBe(0);
        result.Config.Gaps!.Outer.ShouldBe(0);
        result.Config.Workspaces!.PerMonitor.ShouldBe(4);
        result.Config.Bindings.ShouldBeNull();
        result.Config.WindowRules.ShouldBeNull();
    }
}
