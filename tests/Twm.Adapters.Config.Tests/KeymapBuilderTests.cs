using Twm.Application.Commands;
using Twm.Application.InboundPorts;
using Twm.Domain.Geometry;

namespace Twm.Adapters.Config.Tests;

public class KeymapBuilderTests
{
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

    [Fact]
    public void Build_NullConfig_Throws()
    {
        Should.Throw<ArgumentNullException>(() => KeymapBuilder.Build(null!));
    }

    [Fact]
    public void Build_UnknownMod_ReportsErrorButBuildsKeymap()
    {
        // TryParseMod hits the default branch and returns false; Build still
        // falls back to alt and produces the keymap — the error is a warning,
        // not a fatal
        var config = new TwmConfig
        {
            Mod = "hyper",
            Bindings = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["$mod+h"] = "focus left",
            },
        };

        KeymapBuildResult result = KeymapBuilder.Build(config);

        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldContain("hyper");
        result.Errors[0].ShouldContain("alt or win");
        // keymap built with the alt fallback
        result.Keymap.ShouldContainKey(new KeyBinding(ModifierKeys.Alt, 'H'));
    }

    [Fact]
    public void Build_ReconcileDisplaysAction_BridgesToReconcileDisplaysEffect()
    {
        KeymapBuildResult result = KeymapBuilder.Build(
            WithBindings("alt", ("$mod+r", "reconcile-displays"))
        );

        result.Errors.ShouldBeEmpty();
        result.Keymap[new KeyBinding(ModifierKeys.Alt, 'R')].ShouldBeOfType<ReconcileDisplays>();
    }

    [Fact]
    public void Build_MixOfValidAndInvalidBindings_KeepsValidOnesAndReportsAllErrors()
    {
        // partial success: one valid RunCommand binding survives, the three
        // bad ones each contribute an error without sinking the build
        KeymapBuildResult result = KeymapBuilder.Build(
            WithBindings(
                "alt",
                ("$mod+h", "focus left"), // valid
                ("hyper+x", "focus left"), // invalid chord
                ("$mod+g", "get-tree"), // query verb, rejected by TryEffect
                ("$mod+s", "bad-verb") // invalid action
            )
        );

        result.Errors.Count.ShouldBe(3);
        result.Keymap.Count.ShouldBe(1);
        result.Keymap.ShouldContainKey(new KeyBinding(ModifierKeys.Alt, 'H'));
    }

    private static TwmConfig WithBindings(
        string mod,
        params (string Chord, string Action)[] bindings
    ) =>
        new()
        {
            Mod = mod,
            Bindings = bindings.ToDictionary(b => b.Chord, b => b.Action, StringComparer.Ordinal),
        };
}
