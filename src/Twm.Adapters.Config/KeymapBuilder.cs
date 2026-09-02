using System.Diagnostics.CodeAnalysis;
using Twm.Application.Commands;
using Twm.Application.InboundPorts;

namespace Twm.Adapters.Config;

/// <summary>
/// Outcome of building a keymap from config: the map plus any (non-fatal)
/// binding errors.
/// </summary>
public sealed record KeymapBuildResult(
    IReadOnlyDictionary<KeyBinding, KeyEffect> Keymap,
    IReadOnlyList<string> Errors
);

/// <summary>
/// Turns a <see cref="TwmConfig" /> into a <c>KeyBinding -> KeyEffect</c> map.
/// The action string of each binding is parsed by the SAME
/// <see cref="CommandParser" /> grammar as the <c>twm-msg</c> CLI (one grammar
/// for both), then bridged to a <see cref="KeyEffect" />. Absent bindings fall
/// back to the built-in <see cref="DefaultKeymap.DefaultBindings" />. A bad
/// chord/action is collected as an error and skipped, never thrown, one typo
/// can't sink the whole keymap.
/// </summary>
public static class KeymapBuilder
{
    public static KeymapBuildResult Build(TwmConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        List<string> errors = [];

        if (!TryParseMod(config.Mod, out ModifierKeys mod))
        {
            errors.Add($"unknown mod '{config.Mod}' (expected alt or win); using alt");
        }

        IReadOnlyDictionary<string, string> bindings =
            config.Bindings ?? DefaultKeymap.DefaultBindings();

        Dictionary<KeyBinding, KeyEffect> map = [];
        foreach (KeyValuePair<string, string> entry in bindings)
        {
            if (
                !KeyChordParser.TryParse(
                    entry.Key,
                    mod,
                    out KeyBinding binding,
                    out string? chordError
                )
            )
            {
                errors.Add(chordError!);
                continue;
            }

            if (!TryEffect(entry.Value, out KeyEffect? effect, out string? actionError))
            {
                errors.Add($"binding '{entry.Key}': {actionError}");
                continue;
            }

            map[binding] = effect;
        }

        return new KeymapBuildResult(map, errors);
    }

    /// <summary>
    /// Bridges a parsed <see cref="IpcRequest" /> to a
    /// <see cref="KeyEffect" />.
    /// </summary>
    private static bool TryEffect(
        string action,
        [NotNullWhen(true)] out KeyEffect? effect,
        [NotNullWhen(false)] out string? error
    )
    {
        effect = null;
        if (!CommandParser.TryParse(action, out WmRequest? request, out error))
        {
            return false;
        }

        switch (request)
        {
            case RunCommandRequest run:
                effect = new RunCommand(run.Command);
                return true;
            case CloseRequest:
                effect = new CloseFocusedWindow();
                return true;
            case ExitRequest:
                effect = new ExitWm();
                return true;
            case GetTreeRequest:
                error = "'get-tree' is a query, not valid as a keybinding";
                return false;
            default:
                error = $"unsupported action '{action}'";
                return false;
        }
    }

    private static bool TryParseMod(string? mod, out ModifierKeys modifier)
    {
        switch ((mod ?? "alt").ToLowerInvariant())
        {
            case "alt":
                modifier = ModifierKeys.Alt;
                return true;
            case "win":
            case "windows":
                modifier = ModifierKeys.Windows;
                return true;
            default:
                modifier = ModifierKeys.Alt;
                return false;
        }
    }
}
