using Twm.Application.Config;
using Twm.Application.Coordination;
using Twm.Application.InboundPorts;
using Twm.Application.OutboundPorts;
using Twm.Domain.Geometry;
using Twm.Domain.Tiling;
using Twm.Domain.Tree;

namespace Twm.Adapters.Config.Tests;

public class ConfigResolverTests
{
    private static NativeWindowInfo Window(string className) =>
        new(
            Id: new WindowId(1),
            Title: "Editor",
            ClassName: className,
            Bounds: new Rect(0, 0, 800, 600),
            IsVisible: true,
            IsCloaked: false,
            IsToolWindow: false,
            IsMinimized: false
        );

    private static WindowFilter Filter(ResolvedConfig resolved) => new(resolved.WindowRules);

    [Fact]
    public void Resolve_Defaults_ReproducesBuiltInsWithNoErrors()
    {
        ResolvedConfig resolved = ConfigResolver.Resolve(TwmConfig.Defaults, monitorCount: 2);

        resolved.Errors.ShouldBeEmpty();
        resolved.Keymap.Count.ShouldBe(DefaultKeymap.Create().Count);
        resolved.Gaps.ShouldBe(Gaps.None);
        Filter(resolved).IsManageable(Window("Notepad")).ShouldBeTrue();
    }

    [Fact]
    public void Resolve_FullConfig_WiresKeyMapFilterGaps()
    {
        var config = new TwmConfig
        {
            Mod = "alt",
            Gaps = new GapsDto { Inner = 8, Outer = 12 },
            Bindings = new Dictionary<string, string> { ["$mod+h"] = "focus left" },
            WindowRules = [new WindowRuleDto { Class = "Notepad", Action = "ignore" }],
        };

        ResolvedConfig resolved = ConfigResolver.Resolve(config, monitorCount: 1);

        resolved.Errors.ShouldBeEmpty();
        resolved.Keymap.ShouldContainKey(new KeyBinding(ModifierKeys.Alt, 'H'));
        resolved.Gaps.ShouldBe(new Gaps(8, 12));
        Filter(resolved).IsManageable(Window("Notepad")).ShouldBeFalse();
    }

    [Fact]
    public void Resolve_BadBinding_CollectsErrorButKeepsGoodOnes()
    {
        var config = new TwmConfig
        {
            Mod = "alt",
            Bindings = new Dictionary<string, string>
            {
                ["$mod+h"] = "focus left",
                ["hyper+x"] = "focus right",
            },
        };

        ResolvedConfig resolved = ConfigResolver.Resolve(config, monitorCount: 1);

        resolved.Errors.ShouldNotBeEmpty();
        resolved.Keymap.ShouldContainKey(new KeyBinding(ModifierKeys.Alt, 'H'));
    }

    [Fact]
    public void Resolve_TooFewWorkspaceNames_FallsBackWithError()
    {
        var config = new TwmConfig { Workspaces = new WorkspacesDto { Names = ["only-one"] } };

        ResolvedConfig resolved = ConfigResolver.Resolve(config, monitorCount: 2);

        resolved.Errors.ShouldNotBeEmpty();
        resolved.Workspaces!.Names.ShouldBeNull();
    }

    [Fact]
    public void Resolve_DuplicateWorkspaceNames_FallsBackWithError()
    {
        var config = new TwmConfig { Workspaces = new WorkspacesDto { Names = ["a", "b", "a"] } };

        ResolvedConfig resolved = ConfigResolver.Resolve(config, monitorCount: 2);

        resolved.Errors.ShouldNotBeEmpty();
        resolved.Workspaces!.Names.ShouldBeNull();
    }
}
