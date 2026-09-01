namespace Twm.Application.Config;

/// <summary>
/// Resolved, focus-border settings the app consumes (<see cref="Color" /> is
/// a Win32 COLORREF, 0x00BBGGRR). Built from the YAML border section by the
/// config adapter with per-field fallback to <see cref="Defaults" />.
/// </summary>
public sealed record BorderOptions(bool Enabled, uint Color, int Width)
{
    public static BorderOptions Defaults => new(Enabled: true, Color: 0x0000FF00, Width: 3);
}
