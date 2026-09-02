using Twm.Application.Config;

namespace Twm.Adapters.Config.Tests;

public class BorderOptionsTests
{
    private static ResolvedConfig Resolve(TwmConfig config) => ConfigResolver.Resolve(config, 1);

    [Fact]
    public void NoBorderSection_UsesDefaults()
    {
        ResolvedConfig resolved = Resolve(new TwmConfig());

        resolved.Border.ShouldBe(BorderOptions.Defaults);
        resolved.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void FullBorderSection_ParsesEveryField()
    {
        var config = new TwmConfig
        {
            Border = new BorderDto
            {
                Enabled = true,
                Color = "#ff0000",
                Width = 5,
            },
        };

        ResolvedConfig resolved = Resolve(config);

        resolved.Errors.ShouldBeEmpty();
        resolved.Border.Color.ShouldBe(0x000000FFu);
        resolved.Border.Width.ShouldBe(5);
    }

    [Fact]
    public void InvalidColorAndWidth_FallBackWithErrors()
    {
        var config = new TwmConfig
        {
            Border = new BorderDto { Color = "not-a-color", Width = -5 },
        };

        ResolvedConfig resolved = Resolve(config);

        resolved.Errors.Count.ShouldBe(2);
        resolved.Border.Width.ShouldBe(BorderOptions.Defaults.Width);
        resolved.Border.Color.ShouldBe(BorderOptions.Defaults.Color);
    }

    [Fact]
    public void Disabled_IsRespected()
    {
        var config = new TwmConfig { Border = new BorderDto { Enabled = false } };

        ResolvedConfig resolved = Resolve(config);

        resolved.Border.Enabled.ShouldBeFalse();
    }
}
