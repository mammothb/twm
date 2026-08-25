namespace Twm.Config;

public enum CommandKind
{
    // Focus
    FocusLeft,
    FocusRight,
    FocusUp,
    FocusDown,

    // Move focused window
    MoveLeft,
    MoveRight,
    MoveUp,
    MoveDown,

    // Grow focused window toward a direction (~60px)
    ResizeLeft,
    ResizeRight,
    ResizeUp,
    ResizeDown,

    ToggleSplitOrientation,
    CloseFocusedWindow,
    QuitTwm,
}

/// <summary>A fully resolved key combination.</summary>
public readonly record struct KeyCombo(Modifiers Mods, uint VirtualKey);

/// <summary>Thrown for malformed configuration content.</summary>
public class ConfigException : Exception
{
    public ConfigException(string message)
        : base(message) { }
}
