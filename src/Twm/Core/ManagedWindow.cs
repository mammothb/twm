using Twm.Layout;

namespace Twm.Core;

/// <summary>A window twm currently manages.</summary>
public sealed class ManagedWindow
{
    public required nint Hwnd { get; init; }
    public required string Title { get; init; }
    public required string ProcessName { get; init; }

    /// <summary>Monitor the window tiled on (HMONITOR).</summary>
    public required nint Monitor { get; set; }

    /// <summary>Position in its monitor's tree.</summary>
    public required WindowLeaf Leaf { get; set; }
}
