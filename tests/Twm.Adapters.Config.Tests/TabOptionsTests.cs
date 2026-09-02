using Twm.Application.Config;

namespace Twm.Adapters.Config.Tests;

public class TabOptionsTests
{
    private static ResolvedConfig Resolve(TwmConfig config) => ConfigResolver.Resolve(config, 1);

    [Fact]
    public void NoTabsSection_InheritsBarThemeAndDefaultHeight()
    {
        ResolvedConfig resolved = Resolve(new TwmConfig());

        resolved.Tabs.Height.ShouldBe(TabOptions.Defaults.Height);
        resolved.Tabs.Background.ShouldBe(resolved.Bar.Background);
        resolved.Tabs.Foreground.ShouldBe(resolved.Bar.Foreground);
        resolved.Tabs.ActiveBackground.ShouldBe(resolved.Bar.ActiveBackground);
        resolved.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void TabsInheritCustomBarColors_WhenNotOverridden()
    {
        var config = new TwmConfig { Bar = new BarDto { Background = "#ff0000" } };

        ResolvedConfig resolved = Resolve(config);

        resolved.Tabs.Background.ShouldBe(0x000000FFu);
    }

    [Fact]
    public void FullTabsSection_ParsesEveryField()
    {
        var config = new TwmConfig
        {
            Bar = new BarDto { Background = "#101010" },
            Tabs = new TabsDto
            {
                Height = 30,
                Background = "#ff0000",
                Foreground = "#00ff00",
                ActiveBackground = "#0000ff",
            },
        };

        ResolvedConfig resolved = Resolve(config);

        resolved.Errors.ShouldBeEmpty();
        resolved.Tabs.Height.ShouldBe(30);
        resolved.Tabs.Background.ShouldBe(0x000000FFu);
        resolved.Tabs.Foreground.ShouldBe(0x0000FF00u);
        resolved.Tabs.ActiveBackground.ShouldBe(0x00FF0000u);
    }

    [Fact]
    public void InvalidColorAndWidth_FallBackWithErrors()
    {
        var config = new TwmConfig
        {
            Tabs = new TabsDto { Height = -5, Background = "not-a-color" },
        };

        ResolvedConfig resolved = Resolve(config);

        resolved.Errors.Count.ShouldBe(2);
        resolved.Tabs.Height.ShouldBe(TabOptions.Defaults.Height);
        resolved.Tabs.Background.ShouldBe(TabOptions.Defaults.Background);
    }
}
