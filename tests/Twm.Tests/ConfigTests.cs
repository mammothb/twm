using Shouldly;
using Twm.Config;
using Xunit;

namespace Twm.Tests;

public class ConfigTests
{
    private static uint VkLetter(char c) => (uint)(char.ToLowerInvariant(c) - 'a' + 0x41);

    private static uint VkArrow(string name) =>
        name switch
        {
            "left" => 0x25,
            "up" => 0x26,
            "right" => 0x27,
            "down" => 0x28,
            _ => throw new ArgumentException(name),
        };

    [Fact]
    public void Default_yaml_parses_with_expected_bindings()
    {
        var config = ConfigLoader.Parse(ConfigLoader.DefaultYaml);

        config.ModKey.ShouldBe(Modifiers.Alt);
        config.Bindings.ShouldNotBeEmpty();

        config.Bindings[new KeyCombo(Modifiers.Alt, VkLetter('h'))].ShouldBe(CommandKind.FocusLeft);
        config.Bindings[new KeyCombo(Modifiers.Alt, VkLetter('j'))].ShouldBe(CommandKind.FocusDown);
        config
            .Bindings[new KeyCombo(Modifiers.Alt | Modifiers.Shift, VkLetter('h'))]
            .ShouldBe(CommandKind.MoveLeft);
        config
            .Bindings[new KeyCombo(Modifiers.Alt | Modifiers.Ctrl, VkLetter('l'))]
            .ShouldBe(CommandKind.ResizeRight);
        config
            .Bindings[new KeyCombo(Modifiers.Alt, VkArrow("left"))]
            .ShouldBe(CommandKind.FocusLeft);
        config
            .Bindings[new KeyCombo(Modifiers.Alt | Modifiers.Shift, VkLetter('q'))]
            .ShouldBe(CommandKind.CloseFocusedWindow);
        config
            .Bindings[new KeyCombo(Modifiers.Alt | Modifiers.Shift, VkLetter('e'))]
            .ShouldBe(CommandKind.QuitTwm);
    }

    [Fact]
    public void Mod_key_can_be_overridden()
    {
        var yaml = """
            mod_key: ctrl
            keybindings:
              - { trigger: ctrl+q, command: close_focused_window }
            """;

        var config = ConfigLoader.Parse(yaml);
        config.ModKey.ShouldBe(Modifiers.Ctrl);
        config.Bindings.ShouldContainKey(new KeyCombo(Modifiers.Ctrl, VkLetter('q')));
    }

    [Fact]
    public void Commands_parse_case_insensitively()
    {
        var yaml = """
            mod_key: alt
            keybindings:
              - { trigger: alt+x, command: FOCUS_LEFT }
            """;

        ConfigLoader
            .Parse(yaml)
            .Bindings[new KeyCombo(Modifiers.Alt, VkLetter('x'))]
            .ShouldBe(CommandKind.FocusLeft);
    }

    [Fact]
    public void Function_and_named_keys_resolve()
    {
        var yaml = """
            mod_key: alt
            keybindings:
              - { trigger: alt+f5,     command: toggle_split_orientation }
              - { trigger: alt+pgdn,   command: focus_right }
              - { trigger: alt+escape, command: quit_twm }
            """;

        var bindings = ConfigLoader.Parse(yaml).Bindings;
        bindings.ShouldContainKey(new KeyCombo(Modifiers.Alt, 0x74)); // F5
        bindings.ShouldContainKey(new KeyCombo(Modifiers.Alt, 0x22)); // PgDn
        bindings.ShouldContainKey(new KeyCombo(Modifiers.Alt, 0x1B)); // Escape
    }

    [Fact]
    public void Unknown_command_throws_with_the_offending_name()
    {
        var yaml = """
            mod_key: alt
            keybindings:
              - { trigger: alt+x, command: make_coffee }
            """;

        var ex = Should.Throw<ConfigException>(() => ConfigLoader.Parse(yaml));
        ex.Message.ShouldContain("make_coffee");
    }

    [Fact]
    public void Trigger_missing_mod_throws()
    {
        var yaml = """
            mod_key: alt
            keybindings:
              - { trigger: h, command: focus_left }
            """;

        Should
            .Throw<ConfigException>(() => ConfigLoader.Parse(yaml))
            .Message.ShouldContain("mod key");
    }

    [Fact]
    public void Duplicate_trigger_throws()
    {
        var yaml = """
            mod_key: alt
            keybindings:
              - { trigger: alt+h, command: focus_left }
              - { trigger: alt+h, command: focus_right }
            """;

        Should
            .Throw<ConfigException>(() => ConfigLoader.Parse(yaml))
            .Message.ShouldContain("Duplicate");
    }

    [Fact]
    public void Unknown_modifier_throws()
    {
        var yaml = """
            mod_key: alt
            keybindings:
              - { trigger: alt+hyperspace+x, command: focus_left }
            """;

        Should
            .Throw<ConfigException>(() => ConfigLoader.Parse(yaml))
            .Message.ShouldContain("hyperspace");
    }
}
