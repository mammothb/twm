using System.Globalization;
using Twm.Appliation.Config;

namespace Twm.Adapters.Config;

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

        KeymapBuildResult keymapResult = KeymapBuilder.Build(config);
        errors.AddRange(keymapResult.Errors);

        (IReadOnlyList<WindowRule> rules, IReadOnlyList<string> ruleErrors) = WindowRule.Compile(
            config.WindowRules
        );
        errors.AddRange(ruleErrors);

        WorkspacesDto? workspaces = config.Workspaces;
        if (workspaces?.Names is { Count: > 0 } names && names.Count < monitorCount)
        {
            errors.Add(
                $"workspaces.names has {names.Count} entries but there are {monitorCount} monitors; using the default workspace layout."
            );
        }

        return new ResolvedConfig(
            Filter: new WindowFilter(rules),
            Workspaces: workspaces,
            Errors: errors
        );
    }
}
