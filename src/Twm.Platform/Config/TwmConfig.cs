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

    /// <summary>
    /// Extra ignore/manage rules layered on the built-in filter.
    /// Null = defaults only.
    /// </summary>
    public List<WindowRuleDto>? WindowRules { get; set; }
}

[YamlObject]
public partial class WindowRuleDto
{
    public string? Class { get; set; }
    public string? Title { get; set; }

    /// <summary>"ignore" or "manage".</summary>
    public string? Action { get; set; }
}
