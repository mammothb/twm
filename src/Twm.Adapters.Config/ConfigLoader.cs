using System.Text;
using VYaml.Serialization;

namespace Twm.Adapters.Config;

/// <summary>
/// Outcome of loading config: the effective config plus any (non-fatal) parse
/// errors.
/// </summary>
public sealed record ConfigLoadResult(TwmConfig Config, IReadOnlyList<string> Errors);

/// <summary>
/// Parses a YAML config string into a <see cref="TwmConfig" />. Total by
/// design: empty input -> <see cref="TwmConfig.Defaults" />; a parse/type
/// error -> defaults + an error message (never throws). A malformed config must
/// never leave the user with no working WM.
/// </summary>
public static class ConfigLoader
{
    public static ConfigLoadResult Load(string? yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return new ConfigLoadResult(TwmConfig.Defaults, []);
        }

        try
        {
            TwmConfig config = YamlSerializer.Deserialize<TwmConfig>(Encoding.UTF8.GetBytes(yaml));
            return new ConfigLoadResult(config ?? TwmConfig.Defaults, []);
        }
        catch (Exception error)
        {
            // VYaml throws parser/serializer exceptions on malformed input;
            // stay total for a WM
            return new ConfigLoadResult(TwmConfig.Defaults, [error.Message]);
        }
    }
}
