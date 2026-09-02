using Twm.Application.Config;

namespace Twm.Adapters.Config.Tests;

public class BarOptionsTests
{
    private static ResolvedConfig Resolve(TwmConfig config) => ConfigResolver.Resolve(config, 1);

    [Fact]
    public void NoBarSection_UsesDefaults()
    {
        ResolvedConfig resolved = Resolve(new TwmConfig());

        resolved.Bar.ShouldBe(BarOptions.Defaults);
        resolved.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void FullBarSection_ParsesEveryField()
    {
        var config = new TwmConfig
        {
            Bar = new BarDto
            {
                Enabled = true,
                Position = "bottom",
                Height = 32,
                Background = "#101010",
                Foreground = "#ffffff",
                ActiveBackground = "#285577",
                ShowTitle = false,
                ShowClock = true,
            },
        };

        ResolvedConfig resolved = Resolve(config);

        resolved.Errors.ShouldBeEmpty();
        resolved.Bar.Position.ShouldBe(BarPosition.Bottom);
        resolved.Bar.Height.ShouldBe(32);
        resolved.Bar.Background.ShouldBe(0x00101010u);
        resolved.Bar.Foreground.ShouldBe(0x00FFFFFFu);
        resolved.Bar.ActiveBackground.ShouldBe(0x00775528u);
        resolved.Bar.ShowTitle.ShouldBeFalse();
        resolved.Bar.ShowClock.ShouldBeTrue();
    }

    [Fact]
    public void InvalidColorAndPosition_FallBackWithErrors()
    {
        var config = new TwmConfig
        {
            Bar = new BarDto
            {
                Position = "side",
                Height = -5,
                Background = "not-a-color",
            },
        };

        ResolvedConfig resolved = Resolve(config);

        resolved.Errors.Count.ShouldBe(3);
        resolved.Bar.Position.ShouldBe(BarOptions.Defaults.Position);
        resolved.Bar.Height.ShouldBe(BarOptions.Defaults.Height);
        resolved.Bar.Background.ShouldBe(BarOptions.Defaults.Background);
    }

    [Fact]
    public void Disabled_IsRespected()
    {
        var config = new TwmConfig { Bar = new BarDto { Enabled = false } };

        ResolvedConfig resolved = Resolve(config);

        resolved.Bar.Enabled.ShouldBeFalse();
    }
}
