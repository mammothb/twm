namespace Twm.Config;

/// <summary>Parsed, validated configuration ready for runtime use.</summary>
public sealed class TwmConfig
{
    public required Modifiers ModKey { get; init; }

    /// <summary>All resolved key combos → commands.</summary>
    public required IReadOnlyDictionary<KeyCombo, CommandKind> Bindings { get; init; }
}

/// <summary>YAML file shape.</summary>
public sealed class ConfigFile
{
    public string ModKey { get; set; } = "";
    public List<KeybindingEntry> Keybindings { get; set; } = [];
}

public sealed class KeybindingEntry
{
    public string Trigger { get; set; } = "";
    public string Command { get; set; } = "";
}
