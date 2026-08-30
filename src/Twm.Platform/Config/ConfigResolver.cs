using System.Globalization;
using Twm.Core.Layout;

namespace Twm.Platform.Config;

/// <summary>
/// The concrete inputs the WM consumes, resolved from a
/// <see cref="TwmConfig" />.
/// </summary>
public sealed record ResolvedConfig(WindowFilter Filter, IReadOnlyList<string> Errors);

/// <summary>
/// Turns a parsed <see cref="TwmConfig" /> into the concrete WM inputs,
/// aggregating every non-fatal error from the sub-builders. Never throws:
/// invalid pieces are dropped with an error and the corresponding default is
/// used, so the WM always comes up. <paramref name="monitorCount" /> validates
/// explicit workspace names.
/// </summary>
public static class ConfigResolver
{
    public static ResolvedConfig Resolve(TwmConfig config, int monitorCount)
    {
        ArgumentNullException.ThrowIfNull(config);
        List<string> errors = [];

        (IReadOnlyList<WindowRule> rules, IReadOnlyList<string> ruleErrors) = WindowRule.Compile(
            config.WindowRules
        );
        errors.AddRange(ruleErrors);

        return new ResolvedConfig(new WindowFilter(rules), errors);
    }
}
