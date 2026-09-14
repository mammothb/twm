using Twm.Application.Config;
using Twm.Domain.Tiling;

namespace Twm.Adapters.Config;

/// <summary>
/// Maps config DTOs to the Application/Domain value types the WM consumes.
/// </summary>
public static class ConfigMapping
{
    /// <summary>
    /// Converts a <see cref="GapsDto" /> to domain <see cref="Gaps" />. Null
    /// (no <c>gaps:</c> section) -> see <see cref="Gaps.None" />; a missing
    /// inner/outer -> 0.
    /// </summary>
    public static Gaps MapGaps(GapsDto? dto)
    {
        if (dto is null)
        {
            return Gaps.None;
        }

        return new Gaps(dto.Inner ?? 0, dto.Outer ?? 0);
    }

    /// <summary>
    /// Maps a <see cref="WorkspacesDto" /> to the application
    /// <see cref="WorkspaceOptions" /> model (null to null). Topology-dependent
    /// name validation (too few/duplicate) is the resolver's concern.
    /// </summary>
    public static WorkspaceOptions? MapWorkspaces(WorkspacesDto? dto)
    {
        if (dto is null)
        {
            return null;
        }

        return new WorkspaceOptions { PerMonitor = dto.PerMonitor, Names = dto.Names };
    }
}
