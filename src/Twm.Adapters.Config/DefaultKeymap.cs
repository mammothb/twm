using System.Globalization;
using Twm.Application.InboundPorts;

namespace Twm.Adapters.Config;

/// <summary>
/// The built-in i3-style default keymap (mod = Alt): vim h/j/k/l focus
/// (crossing monitors at a workspace edge), +Shift to move, Alt+Ctrl+h/j/k/l
/// to resize (Right/Down grow, Left/Up shrink), Alt+E to flip the split
/// direction, Alt+V/B to split, Alt+1..8 to switch workspace, Alt+Shift+1..8
/// to move a window to a workspace, Alt+Shift+Q to close, Alt+Shift+E to exit.
/// </summary>
public static class DefaultKeymap
{
    /// <summary>
    /// Workspace hotkeys Alt+1..8, matching the default 8-workspace (2-monitor)
    /// layout.
    /// </summary>
    private const int WorkspaceKeys = 8;

    /// <summary>
    /// Builds the default keymap by running the built-in bindings through the
    /// shared builder.
    /// </summary>
    public static IReadOnlyDictionary<KeyBinding, KeyEffect> Create()
    {
        KeymapBuildResult result = KeymapBuilder.Build(
            new TwmConfig { Mod = "alt", Bindings = DefaultBindings() }
        );

        // The built-in bindings must always be valid; fail loudly if a future
        // edit breaks one
        if (result.Errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Built-in default keymap is invalid: " + string.Join("; ", result.Errors)
            );
        }

        return result.Keymap;
    }

    /// <summary>
    /// The default bindings in config form (chord -> command string). Single
    /// source of truth for the default keymap; also used as the fallback when a
    /// config omits <c>bindings:</c>.
    /// </summary>
    public static Dictionary<string, string> DefaultBindings()
    {
        var bindings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["$mod+h"] = "focus left",
            ["$mod+j"] = "focus down",
            ["$mod+k"] = "focus up",
            ["$mod+l"] = "focus right",
            ["$mod+shift+h"] = "move left",
            ["$mod+shift+j"] = "move down",
            ["$mod+shift+k"] = "move up",
            ["$mod+shift+l"] = "move right",
            ["$mod+ctrl+h"] = "resize left",
            ["$mod+ctrl+j"] = "resize down",
            ["$mod+ctrl+k"] = "resize up",
            ["$mod+ctrl+l"] = "resize right",
            ["$mod+e"] = "toggle-split",
            ["$mod+s"] = "layout stacked",
            ["$mod+w"] = "layout tabbed",
            ["$mod+v"] = "split v",
            ["$mod+b"] = "split h",
            ["$mod+shift+q"] = "close",
            ["$mod+shift+e"] = "exit",
        };

        for (int number = 1; number <= WorkspaceKeys; number++)
        {
            string name = number.ToString(CultureInfo.InvariantCulture);
            bindings[$"$mod+{name}"] = $"workspace {name}";
            bindings[$"$mod+shift+{name}"] = $"move-to-workspace {name}";
        }

        return bindings;
    }
}
