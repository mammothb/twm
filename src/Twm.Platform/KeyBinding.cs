namespace Twm.Platform;

/// <summary>
/// Keyboard modifiers. Bit values deliberately match the Win32 <c>MOD_*</c>
/// constants so the Windows layer can cast this straight to the
/// <c>RegisterHotKey</c> modifier flags.
/// </summary>
[Flags]
public enum ModifierKeys
{
    None = 0,
    Alt = 1, // MOD_ALT
    Control = 2, // MOD_CONTROL
    Shift = 3, // MOD_SHIFT
    Windows = 8, // MOD_WIN
}

/// <summary>
/// A hotkey: a set of modifiers plus a single key identified by its Win32
/// <b>virtual-key code</b>. For A-Z and 0-9 the VK equals the uppercase ASCII
/// value ('H' == 0x48), so a <c>char</c> literal implicitly converts
/// (<c>new KeyBinding(ModifierKeys.Alt, 'H')</c> still works). Named/OEM keys
/// (backslash = 0xDC, minus = 0xBD, F1 = 0x70, arrows ...) resolve via
/// <see cref="Config.KeyChordParser" />. The platform layer passes
/// <see cref="VirtualKey" /> straight to <c>RegisterHotKey</c>.
/// </summary>
public readonly record struct KeyBinding(ModifierKeys Modifiers, uint VirtualKey);
