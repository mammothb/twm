using Twm.Application.Config;
using Twm.Application.OutboundPorts;

namespace Twm.Adapters.Config;

/// <summary>
/// The YAML file <see cref="IConfigSource" />: reads the config file (default
/// <see cref="ConfigPaths.Default" />, parses it with
/// <see cref="ConfigLoader" />, and resolves it with
/// <see cref="ConfigResolver" /> into a <see cref="ResolvedConfig" />. A
/// missing file yields the built-in default; parse errors are surfaced in
/// <see cref="ResolvedConfig.Errors" /> alongside the resolver's, never thrown.
/// </summary>
public sealed class YamlConfigSource(string? path = null) : IConfigSource
{
    private readonly string _path = path ?? ConfigPaths.Default();

    public ResolvedConfig Load(int monitorCount)
    {
        string? yaml = File.Exists(_path) ? File.ReadAllText(_path) : null;
        ConfigLoadResult loadResult = ConfigLoader.Load(yaml);
        ResolvedConfig resolved = ConfigResolver.Resolve(loadResult.Config, monitorCount);

        if (loadResult.Errors.Count == 0)
        {
            return resolved;
        }

        return resolved with
        {
            Errors = [.. loadResult.Errors, .. resolved.Errors],
        };
    }
}
