namespace Twm;

/// <summary>Modifier-key set, used by config parsing and the keyboard hook.</summary>
[Flags]
public enum Modifiers
{
    None = 0,
    Alt = 1,
    Shift = 2,
    Ctrl = 4,
    Win = 8,
}
