using System.Globalization;
using Twm.Application.Config;
using Twm.Domain.Tiling;

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

        WindowRuleCompileResult rulesResult = ConfigMapping.CompileRules(config.WindowRules);
        errors.AddRange(rulesResult.Errors);

        Gaps gaps = ConfigMapping.MapGaps(config.Gaps);

        WorkspaceOptions? workspaces = ConfigMapping.MapWorkspaces(config.Workspaces);
        if (workspaces?.Names is { Count: > 0 } names)
        {
            if (names.Count < monitorCount)
            {
                errors.Add(
                    $"workspaces.names has {names.Count} entries but there are {monitorCount} monitors; using the default workspace layout."
                );
                workspaces = new WorkspaceOptions { PerMonitor = workspaces.PerMonitor };
            }
            else if (TryFindDuplicate(names, out string duplicate))
            {
                errors.Add(
                    $"workspaces.names has a duplicate entry '{duplicate}'; using the default workspace layout."
                );
                workspaces = new WorkspaceOptions { PerMonitor = workspaces.PerMonitor };
            }
        }

        BarOptions bar = ResolveBar(config.Bar, errors);
        BorderOptions border = ResolveBorder(config.Border, errors);
        TabOptions tabs = ResolveTabs(config.Tabs, bar, errors);

        return new ResolvedConfig(
            Keymap: keymapResult.Keymap,
            WindowRules: rulesResult.Rules,
            Gaps: gaps,
            Workspaces: workspaces,
            Bar: bar,
            Border: border,
            Tabs: tabs,
            Errors: errors
        );
    }

    private static TabOptions ResolveTabs(TabsDto? dto, BarOptions bar, List<string> errors)
    {
        // Tabs default to the resolved status-bar theme (so they match the bar)
        // height defaults to the layout engine's stripe size
        int height = TabOptions.Defaults.Height;
        if (dto is null)
        {
            return new TabOptions(height, bar.Background, bar.Foreground, bar.ActiveBackground);
        }

        if (dto.Height is int h)
        {
            if (h > 0)
            {
                height = h;
            }
            else
            {
                errors.Add($"tabs.height {h} invalid (must be > 0); using {height}");
            }
        }

        return new TabOptions(
            Height: height,
            Background: ParseColor(dto.Background, bar.Background, "tabs.background", errors),
            Foreground: ParseColor(dto.Foreground, bar.Foreground, "tabs.foreground", errors),
            ActiveBackground: ParseColor(
                dto.ActiveBackground,
                bar.ActiveBackground,
                "tabs.activeBackground",
                errors
            )
        );
    }

    private static BorderOptions ResolveBorder(BorderDto? dto, List<string> errors)
    {
        BorderOptions d = BorderOptions.Defaults;
        if (dto is null)
        {
            return d;
        }

        int width = d.Width;
        if (dto.Width is int w)
        {
            if (w > 0)
            {
                width = w;
            }
            else
            {
                errors.Add($"border.width {w} invalid (must be > 0); using {width}");
            }
        }

        return new BorderOptions(
            Enabled: dto.Enabled ?? d.Enabled,
            Color: ParseColor(dto.Color, d.Color, "border.color", errors),
            Width: width
        );
    }

    private static BarOptions ResolveBar(BarDto? dto, List<string> errors)
    {
        BarOptions d = BarOptions.Defaults;
        if (dto is null)
        {
            return d;
        }

        BarPosition position = d.Position;
        if (dto.Position is string p)
        {
            switch (p.Trim().ToLowerInvariant())
            {
                case "top":
                    position = BarPosition.Top;
                    break;
                case "bottom":
                    position = BarPosition.Bottom;
                    break;
                default:
                    errors.Add($"bar.position '{p}' invalid (top|bottom); using {position}");
                    break;
            }
        }

        int height = d.Height;
        if (dto.Height is int h)
        {
            if (h > 0)
            {
                height = h;
            }
            else
            {
                errors.Add($"bar.height {h} invalid (must be > 0); using {height}");
            }
        }

        return new BarOptions(
            Enabled: dto.Enabled ?? d.Enabled,
            Position: position,
            Height: height,
            Background: ParseColor(dto.Background, d.Background, "bar.background", errors),
            Foreground: ParseColor(dto.Foreground, d.Foreground, "bar.foreground", errors),
            ActiveBackground: ParseColor(
                dto.ActiveBackground,
                d.ActiveBackground,
                "bar.activeBackground",
                errors
            ),
            ShowTitle: dto.ShowTitle ?? d.ShowTitle,
            ShowClock: dto.ShowClock ?? d.ShowClock
        );
    }

    /// <summary>
    /// Parses <c>#RRGGBB</c> (or <c>RRGGBB</c>) to a Win32 COLOREF
    /// (0x00BBGGRR).
    /// </summary>
    private static uint ParseColor(string? hex, uint fallback, string field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return fallback;
        }

        string trimmed = hex.Trim().TrimStart('#');
        if (
            trimmed.Length == 6
            && uint.TryParse(
                trimmed,
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out uint rgb
            )
        )
        {
            uint r = (rgb >> 16) & 0xFF;
            uint g = (rgb >> 8) & 0xFF;
            uint b = rgb & 0xFF;
            return r | (g << 8) | (b << 16);
        }

        errors.Add($"{field} '{hex}' invalid (expected #RRGGBB); using default");
        return fallback;
    }

    private static bool TryFindDuplicate(IReadOnlyList<string> names, out string duplicate)
    {
        var seen = new HashSet<string>(names.Count, StringComparer.Ordinal);
        foreach (string name in names)
        {
            if (!seen.Add(name))
            {
                duplicate = name;
                return true;
            }
        }
        duplicate = "";
        return false;
    }
}
