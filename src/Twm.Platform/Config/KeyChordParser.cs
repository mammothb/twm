using System.Collections.Frozen;

namespace Twm.Platform.Config;

/// <summary>
/// Parses a key chord like <c>"$mod+shift+h"</c> into a
/// <see cref="KeyBinding" />. The final token is either a single A-Z/0-9
/// character (VK = its uppercase ASCII value) or an i3-style key name
/// (<c>backslash</c>, etc) resolved to its Win32 virtual-key code.
/// </summary>
public static class KeyChordParser
{
    /// <summary>
    /// i3-keysym-style key names -> Win32 virtual-key codes (WinUser.h VK_*).
    /// </summary>
    private static readonly FrozenDictionary<string, uint> s_namedKeys = BuildNamedKeys();

    public static bool TryParse(
        string chord,
        ModifierKeys mod,
        out KeyBinding binding,
        out string? error
    )
    {
        binding = default;
        error = null;

        string[] parts = (chord ?? "").Split(
            '+',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        if (parts.Length == 0)
        {
            error = $"empty key chord '{chord}'";
            return false;
        }

        ModifierKeys modifiers = ModifierKeys.None;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!TryParseModifier(parts[i], mod, out ModifierKeys parsed))
            {
                error = $"unknown modifier '{parts[i]}' in chord '{chord}'";
                return false;
            }

            modifiers |= parsed;
        }

        string keyToken = parts[^1];
        if (!TryResolveKey(keyToken, out uint virtualKey))
        {
            error =
                $"unsupported key '{keyToken}' in chord '{chord}' (a single A-Z/0-9, or a name like backslash, minus, f1, left)";
            return false;
        }

        binding = new KeyBinding(modifiers, virtualKey);
        return true;
    }

    private static bool TryParseModifier(string token, ModifierKeys mod, out ModifierKeys modifier)
    {
        switch (token.ToLowerInvariant())
        {
            case "$mod":
                modifier = mod;
                return true;
            case "alt":
                modifier = ModifierKeys.Alt;
                return true;
            case "ctrl":
            case "control":
                modifier = ModifierKeys.Control;
                return true;
            case "shift":
                modifier = ModifierKeys.Shift;
                return true;
            case "win":
            case "windows":
                modifier = ModifierKeys.Windows;
                return true;
            default:
                modifier = ModifierKeys.None;
                return false;
        }
    }

    private static bool TryResolveKey(string token, out uint virtualKey)
    {
        string trimmed = token.Trim();
        if (trimmed.Length == 1)
        {
            char c = char.ToUpperInvariant(trimmed[0]);
            if (('A' <= c && c <= 'Z') || ('0' <= c && c <= '9'))
            {
                virtualKey = c;
                return true;
            }
        }
        return s_namedKeys.TryGetValue(trimmed.ToLowerInvariant(), out virtualKey);
    }

    private static FrozenDictionary<string, uint> BuildNamedKeys()
    {
        var map = new Dictionary<string, uint>(StringComparer.Ordinal)
        {
            ["minus"] = 0xBD, // VK_OEM_MINUS (-_)
            ["equal"] = 0xBB, // VK_OEM_PLUS (=+)
            ["comma"] = 0xBC, // VK_OEM_COMMA (,<)
            ["period"] = 0xBE, // VK_OEM_PERIOD (.>)
            ["semicolon"] = 0xBA, // VK_OEM_1 (;:)
            ["slash"] = 0xBF, // VK_OEM_2 (/?)
            ["grave"] = 0xC0, // VK_OEM_3 (`~)
            ["bracketleft"] = 0xDB, // VK_OEM_4 ([{)
            ["backslash"] = 0xDC, // VK_OEM_5 (\|)
            ["bracketright"] = 0xDD, // VK_OEM_6 (]})
            ["apostrophe"] = 0xDE, // VK_OEM_7 ('")
            ["return"] = 0x0D, // VK_RETURN
            ["enter"] = 0x0D,
            ["backspace"] = 0x08, // VK_BACK
            ["tab"] = 0x09, // VK_TAB
            ["escape"] = 0x1B, // VK_ESCAPE
            ["esc"] = 0x1B,
            ["space"] = 0x20, // VK_SPACE
            ["pageup"] = 0x21, // VK_PRIOR
            ["pagedown"] = 0x22, // VK_NEXT
            ["end"] = 0x23, // VK_END
            ["home"] = 0x24, // VK_HOME
            ["left"] = 0x25, // VK_LEFT
            ["up"] = 0x26, // VK_UP
            ["right"] = 0x27, // VK_RIGHT
            ["down"] = 0x28, // VK_DOWN
            ["insert"] = 0x2D, // VK_INSERT
            ["delete"] = 0x2E, // VK_DELETE
        };

        for (uint f = 1; f <= 12; f++)
        {
            map[$"f{f}"] = 0x70 + (f - 1); // VK_F1 (0x70) .. VK_F12 (0x7B)
        }

        return map.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
