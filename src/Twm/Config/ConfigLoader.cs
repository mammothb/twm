using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Twm.Config;

/// <summary>Loads and parses twm.yaml into a validated <see cref="TwmConfig"/>.</summary>
public static class ConfigLoader
{
    /// <summary>%USERPROFILE%\.config\twm\config.yaml</summary>
    public static readonly string DefaultPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config",
        "twm",
        "config.yaml"
    );

    public static TwmConfig Load()
    {
        string? path = Environment.GetEnvironmentVariable("TWM_CONFIG");
        bool explicitPath = !string.IsNullOrEmpty(path);

        if (!explicitPath && !File.Exists(DefaultPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DefaultPath)!);
            File.WriteAllText(DefaultPath, DefaultYaml);
            Log.Info($"No config found — wrote defaults to {DefaultPath}");
            return Parse(DefaultYaml);
        }

        string file = explicitPath ? path! : DefaultPath;
        Log.Info($"Loading config: {file}");
        return Parse(File.ReadAllText(file));
    }

    public static TwmConfig Parse(string yaml)
    {
        ConfigFile file;
        try
        {
            file =
                new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build()
                    .Deserialize<ConfigFile>(yaml)
                ?? new ConfigFile();
        }
        catch (Exception ex)
        {
            throw new ConfigException($"YAML syntax error: {ex.Message}");
        }

        Modifiers modKey = ParseSingleModifier(
            file.ModKey.Length > 0 ? file.ModKey : "alt",
            "mod_key"
        );

        var bindings = new Dictionary<KeyCombo, CommandKind>();
        foreach (KeybindingEntry entry in file.Keybindings)
        {
            KeyCombo combo = ParseTrigger(entry.Trigger, modKey, out Modifiers extraMods);
            if (!TryParseCommand(entry.Command, out CommandKind command))
            {
                throw new ConfigException(
                    $"Unknown command '{entry.Command}' for trigger '{entry.Trigger}'."
                );
            }

            if (extraMods.HasFlag(Modifiers.Alt) || extraMods.HasFlag(Modifiers.Win))
            {
                throw new ConfigException($"Trigger '{entry.Trigger}' rebinds the mod key itself.");
            }

            combo = new KeyCombo(combo.Mods | extraMods, combo.VirtualKey);
            if (bindings.ContainsKey(combo))
            {
                throw new ConfigException($"Duplicate binding for trigger '{entry.Trigger}'.");
            }

            bindings[combo] = command;
        }

        return new TwmConfig { ModKey = modKey, Bindings = bindings };
    }

    /// <summary>"focus_left" / "FocusLeft" / "focus-left" all match.</summary>
    private static bool TryParseCommand(string text, out CommandKind command)
    {
        string normalized = text.Replace("_", "").Replace("-", "").Trim();
        foreach (CommandKind kind in Enum.GetValues<CommandKind>())
        {
            if (string.Equals(kind.ToString(), normalized, StringComparison.OrdinalIgnoreCase))
            {
                command = kind;
                return true;
            }
        }
        command = default;
        return false;
    }

    private static Modifiers ParseSingleModifier(string token, string what)
    {
        Modifiers? mods =
            token.ToLowerInvariant() switch
            {
                "alt" or "lalt" or "ralt" or "mod" => Modifiers.Alt,
                "ctrl" or "control" => Modifiers.Ctrl,
                "shift" => Modifiers.Shift,
                "win" or "super" or "meta" or "logo" => Modifiers.Win,
                _ => (Modifiers?)null,
            }
            ?? throw new ConfigException(
                $"'{what}' must be exactly one of: alt, ctrl, shift, win. Got '{token}'."
            );
        return mods.Value;
    }

    /// <summary>"alt+shift+h" → mods split from the final key token.</summary>
    private static KeyCombo ParseTrigger(string trigger, Modifiers modKey, out Modifiers extraMods)
    {
        if (string.IsNullOrWhiteSpace(trigger))
        {
            throw new ConfigException("Empty keybinding trigger.");
        }

        string[] parts = trigger.Split(
            '+',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
        );
        if (parts.Length < 2)
        {
            throw new ConfigException(
                $"Trigger '{trigger}' must include the mod key, e.g. 'alt+h'."
            );
        }

        Modifiers mods = Modifiers.None;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            Modifiers m = ParseModifiers(parts[i]);
            if (m == Modifiers.None)
            {
                throw new ConfigException($"Unknown modifier '{parts[i]}' in trigger '{trigger}'.");
            }

            mods |= m;
        }

        extraMods = mods & ~modKey;
        if ((mods & modKey) == 0)
        {
            throw new ConfigException(
                $"Trigger '{trigger}' does not use the configured mod key ({modKey})."
            );
        }

        string keyToken = parts[^1];
        if (!TryMapVirtualKey(keyToken, out uint vk))
        {
            throw new ConfigException($"Unknown key '{keyToken}' in trigger '{trigger}'.");
        }

        return new KeyCombo(modKey, vk);
    }

    private static Modifiers ParseModifiers(string token)
    {
        return token.ToLowerInvariant() switch
        {
            "alt" or "lalt" or "ralt" or "mod" => Modifiers.Alt,
            "shift" => Modifiers.Shift,
            "ctrl" or "control" or "lctrl" or "rctrl" => Modifiers.Ctrl,
            "win" or "super" or "meta" or "logo" => Modifiers.Win,
            _ => Modifiers.None,
        };
    }

    private static bool TryMapVirtualKey(string token, out uint vk)
    {
        string t = token.ToLowerInvariant();

        if (t.Length == 1)
        {
            char c = t[0];
            if (c is >= 'a' and <= 'z')
            {
                vk = (uint)(c - 'a' + 0x41);
                return true;
            }
            if (c is >= '0' and <= '9')
            {
                vk = (uint)(c - '0' + 0x30);
                return true;
            }
        }

        vk = t switch
        {
            "left" => 0x25,
            "up" => 0x26,
            "right" => 0x27,
            "down" => 0x28,
            "pgup" or "pageup" => 0x21,
            "pgdn" or "pagedown" => 0x22,
            "home" => 0x24,
            "end" => 0x23,
            "tab" => 0x09,
            "enter" or "return" => 0x0D,
            "esc" or "escape" => 0x1B,
            "space" or "spacebar" => 0x20,
            "backspace" => 0x08,
            "delete" or "del" => 0x2E,
            "insert" or "ins" => 0x2D,
            "grave" or "backquote" => 0xC0,
            "minus" => 0xBD,
            "equal" => 0xBB,
            _ when t.Length == 2
                    && t[0] == 'f'
                    && byte.TryParse(t.AsSpan(1), out byte f)
                    && f is >= 1 and <= 24 => (uint)(0x70 + f - 1),
            _ => 0,
        };
        return vk != 0;
    }

    public static string DefaultYaml =>
        """
            # twm configuration
            # Reference: https://github.com/your-name/twm (see README)

            # The primary modifier all triggers start with: alt | ctrl | shift | win
            mod_key: alt

            keybindings:
              # Focus movement (vim keys + arrows)
              - { trigger: alt+h,          command: focus_left }
              - { trigger: alt+j,          command: focus_down }
              - { trigger: alt+k,          command: focus_up }
              - { trigger: alt+l,          command: focus_right }
              - { trigger: alt+left,       command: focus_left }
              - { trigger: alt+down,       command: focus_down }
              - { trigger: alt+up,         command: focus_up }
              - { trigger: alt+right,      command: focus_right }

              # Move the focused window
              - { trigger: alt+shift+h,    command: move_left }
              - { trigger: alt+shift+j,    command: move_down }
              - { trigger: alt+shift+k,    command: move_up }
              - { trigger: alt+shift+l,    command: move_right }
              - { trigger: alt+shift+left,  command: move_left }
              - { trigger: alt+shift+down,  command: move_down }
              - { trigger: alt+shift+up,    command: move_up }
              - { trigger: alt+shift+right, command: move_right }

              # Resize: grow toward a direction by ~60px
              - { trigger: alt+ctrl+h,     command: resize_left }
              - { trigger: alt+ctrl+j,     command: resize_down }
              - { trigger: alt+ctrl+k,     command: resize_up }
              - { trigger: alt+ctrl+l,     command: resize_right }

              # Layout / window management
              - { trigger: alt+t,          command: toggle_split_orientation }
              - { trigger: alt+shift+q,    command: close_focused_window }

              # Quit twm entirely
              - { trigger: alt+shift+e,    command: quit_twm }
            """;
}
