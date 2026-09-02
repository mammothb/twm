using Twm.Application.Commands;
using Twm.Application.InboundPorts;
using Twm.Domain.Geometry;

namespace Twm.Adapters.Config.Tests;

public class KeymapBuilderTests
{
    private static TwmConfig WithBindings(
        string mod,
        params (string Chord, string Action)[] bindings
    ) =>
        new()
        {
            Mod = mod,
            Bindings = bindings.ToDictionary(b => b.Chord, b => b.Action, StringComparer.Ordinal),
        };

    [Fact]
    public void Build_RunCommandBinding_ParsesActionViaCommandGrammar()
    {
        KeymapBuildResult result = KeymapBuilder.Build(
            WithBindings("alt", ("$mod+h", "focus left"))
        );

        result.Errors.ShouldBeEmpty();
        RunCommand run = result
            .Keymap[new KeyBinding(ModifierKeys.Alt, 'H')]
            .ShouldBeOfType<RunCommand>();
        FocusInDirectionCommand cmd = run.Command.ShouldBeOfType<FocusInDirectionCommand>();
        cmd.Direction.ShouldBe(Direction.Left);
    }

    [Fact]
    public void Build_ModWin_ResolvesDollarModToWindows()
    {
        KeymapBuildResult result = KeymapBuilder.Build(
            WithBindings("win", ("$mod+h", "focus left"))
        );

        result.Errors.ShouldBeEmpty();
        result.Keymap.ShouldContainKey(new KeyBinding(ModifierKeys.Windows, 'H'));
    }

    [Fact]
    public void Build_CloseAndExit_BridgeToApplLevelEffects()
    {
        KeymapBuildResult result = KeymapBuilder.Build(
            WithBindings("alt", ("$mod+shift+q", "close"), ("$mod+shift+e", "exit"))
        );

        result.Errors.ShouldBeEmpty();
        result
            .Keymap[new KeyBinding(ModifierKeys.Alt | ModifierKeys.Shift, 'Q')]
            .ShouldBeOfType<CloseFocusedWindow>();
        result
            .Keymap[new KeyBinding(ModifierKeys.Alt | ModifierKeys.Shift, 'E')]
            .ShouldBeOfType<ExitWm>();
    }

    [Fact]
    public void Build_GetTreeBinding_IsRejected()
    {
        KeymapBuildResult result = KeymapBuilder.Build(WithBindings("alt", ("$mod+g", "get-tree")));

        result.Errors.ShouldNotBeEmpty();
        result.Keymap.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("hyper+x", "focus left")]
    [InlineData("$mod+x", "bad-verb")]
    public void Build_InvalidBinding_CollectsErrorAndSkips(string chord, string action)
    {
        KeymapBuildResult result = KeymapBuilder.Build(WithBindings("alt", (chord, action)));

        result.Errors.ShouldNotBeEmpty();
        result.Keymap.ShouldBeEmpty();
    }

    [Fact]
    public void Build_NullBindings_ReproducesDefaultKeymap()
    {
        KeymapBuildResult result = KeymapBuilder.Build(new TwmConfig { Mod = "alt" });

        result.Errors.ShouldBeEmpty();
        result.Keymap.ShouldBe(DefaultKeymap.Create());
    }
}
