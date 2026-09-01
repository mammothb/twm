using Twm.Domain.Tiling;

namespace Twm.Application.Config;

/// <summary>
/// Resolved, tab/stack-bar settings (colors are Win32 COLORREFs, 0x00BBGGRR).
/// Built from the YAML tabs section by the config adapter with per-field
/// fallback to <see cref="Defaults" />.
/// </summary>
public sealed record TabOptions(int Height, uint Background, uint Foreground, uint ActiveBackground)
{
    public static TabOptions Defaults =>
        new(
            Height: LayoutEngine.DefaultTitleBarHeight,
            Background: 0x00303030,
            Foreground: 0x00E0E0E0,
            ActiveBackground: 0x00775528
        );
}
