using VYaml.Annotations;

namespace Twm.Platform.Config;

/// <summary>
/// The deserialized Twm config file. Every section is nullable so "absent" is
/// distinguishable from a set value, consumers fall back to built-in defaults
/// per absent section, giving override-with-fallback. Parsed by VYaml's bundled
/// source generator (AOT-safe); DTOs must be <c>partial</c> and carry
/// <see cref="YamlObjectAttribute" />. Default naming is lowerCamelCase, e.g.,
/// <see cref="WindowRules" /> to <c>windowRules</c>.
/// </summary>
[YamlObject]
public partial class TwmConfig
{
    /// <summary>
    /// Modifier for <c>$mod</c> in bindings: "alt" (default) or "win".
    /// </summary>
    public string? Mod { get; set; }
    public GapsDto? Gaps { get; set; }
    public WorkspacesDto? Workspaces { get; set; }

    ///<summary>
    /// Chord -> command string, e.g., <c>"$mod+h": "focus left"</c>.
    /// Null = built-in keymap.
    /// </summary>
    public Dictionary<string, string>? Bindings { get; set; }

    /// <summary>
    /// Extra ignore/manage rules layered on the built-in filter.
    /// Null = defaults only.
    /// </summary>
    public List<WindowRuleDto>? WindowRules { get; set; }
    public BarDto? Bar { get; set; }
    public BorderDto? Border { get; set; }
    public TabsDto? Tabs { get; set; }

    /// <summary>
    /// Canonical defaults == today's hardcoded behavior, used when the file is
    /// absent or invalid. <see cref="Bindings" />/<see cref="WindowRules" />
    /// stay null (=> built-in keymap/filter).
    /// </summary>
    public static TwmConfig Defaults =>
        new()
        {
            Mod = "alt",
            Gaps = new GapsDto { Inner = 0, Outer = 0 },
            Workspaces = new WorkspacesDto { PerMonitor = 4 },
            Bindings = null,
            WindowRules = null,
        };
}

/// <summary>Gap sizes in pixels; either may be omitted.</summary>
[YamlObject]
public partial class GapsDto
{
    public int? Inner { get; set; }
    public int? Outer { get; set; }
}

/// <summary>
/// Workspace layout: a per-monitor count and/or an explicit orderd name list.
/// </summary>
[YamlObject]
public partial class WorkspacesDto
{
    /// <summary>Workspaces created on each monitor (default 4).</summary>
    public int? PerMonitor { get; set; }

    /// <summary>
    /// Explicit workspace names, distributed round-robin across monitors.
    /// Overrides count.
    /// </summary>
    public List<string>? Names { get; set; }
}

/// <summary>
/// A window rule: match on class and/or title, then apply and action.
/// </summary>
[YamlObject]
public partial class WindowRuleDto
{
    public string? Class { get; set; }
    public string? Title { get; set; }

    /// <summary>"ignore" or "manage".</summary>
    public string? Action { get; set; }
}

/// <summary>
/// Status-bar config. Colors are <c>#RRGGBB</c>; any field may be omitted.
/// </summary>
[YamlObject]
public partial class BarDto
{
    public bool? Enabled { get; set; }

    /// <summary>"top" (default) or "bottom".</summary>
    public string? Position { get; set; }
    public int? Height { get; set; }
    public string? Background { get; set; }
    public string? Foreground { get; set; }
    public string? ActiveBackground { get; set; }
    public bool? ShowTitle { get; set; }
    public bool? ShowClock { get; set; }
}

/// <summary>
/// Focus-border config (overlay). <see cref="Color" /> is <c>#RRGGBB</c>; any
/// field may be omitted.
/// </summary>
[YamlObject]
public partial class BorderDto
{
    public bool? Enabled { get; set; }
    public string? Color { get; set; }
    public int? Width { get; set; }
}

/// <summary>
/// Tab/stack-bar config. Colors are <c>#RRGGBB</c>; any omitted field inherits
/// the status-bar theme (colors) or the default height.
/// </summary>
[YamlObject]
public partial class TabsDto
{
    public int? Height { get; set; }
    public string? Background { get; set; }
    public string? Foreground { get; set; }
    public string? ActiveBackground { get; set; }
}
