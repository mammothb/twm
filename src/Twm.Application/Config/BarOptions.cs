namespace Twm.Application.Config;

/// <summary>
/// Resolved, strongly-typed status-bar settings the app consumes (colors are
/// Win32 COLORREFs, 0x00BBGGRR). Built from the YAML bar section by the config
/// adapter with per-field fallback to <see cref="Defaults" />.
/// </summary>
public sealed record BarOptions(
    bool Enabled,
    BarPosition Position,
    int Height,
    uint Background,
    uint Foreground,
    uint ActiveBackground,
    bool ShowTitle,
    bool ShowClock
)
{
    public static BarOptions Defaults =>
        new(
            Enabled: true,
            Position: BarPosition.Top,
            Height: 28,
            Background: 0x00303030,
            Foreground: 0x00E0E0E0,
            ActiveBackground: 0x00775528,
            ShowTitle: true,
            ShowClock: true
        );
}
