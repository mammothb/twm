using Twm.Application.Config;

namespace Twm.Application.OutboundPorts;

/// <summary>
/// Supplies the resolved config the WM consumes, implemented by the config
/// adapter (YAML loader + resolver). Takes the live monitor count so the
/// resolver can soft-validate explicit workspace names (too few/duplicate)
/// against the topology and fall back to default rather than fail.
/// </summary>
public interface IConfigSource
{
    ResolvedConfig Load(int monitorCount);
}
