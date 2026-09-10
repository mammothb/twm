using Twm.Domain.Geometry;
using Twm.Domain.Tree;

namespace Twm.Application.OutboundPorts;

/// <summary>
/// A snapshot of one top-leve OS window at enumeration time. Pure data: the
/// window filter decides manageability of these fields alone, and the core only
/// ever sees the opaque <see cref="WindowId" />, never a native handle or Win32
/// struct.
/// </summary>
public sealed record NativeWindowInfo(
    WindowId Id,
    string Title,
    string ClassName,
    Rect Bounds,
    bool IsVisible,
    bool IsCloaked,
    bool IsToolWindow,
    bool IsMinimized,
    bool IsChild = false,
    bool IsElevated = false,
    bool IsNoActivate = false,
    bool IsMenuPopup = false,
    bool IsLayered = false,
    /// <summary>
    /// Allowlist-criteria diagnostics (require WS_CAPTION|WS_EX_WINDOWEDGE).
    /// Default true so fakes/tests read as normal; only the Win32 backend sets
    /// them per-window
    /// </summary>
    bool HasCaption = true,
    bool HasWindowEdge = true,
    /// <summary>
    /// HWND of this window's owner (GW_OWNER), or null. Owned windows (modal
    /// dialogs, popups) are hidden by DWM when their owner is cloaked. Only the
    /// Win32 backend sets it.
    /// </summary>
    WindowId? Owner = null,
    /// <summary>
    /// WS_EX_DLGMODALFRAME: thin/double dialog border. Treated as ineligible for
    /// tiling (see WindowFilter). Default false for test fakes; the Win32
    /// backend fills it from GWL_EXSTYLE.
    /// </summary>
    bool IsDlgModalFrame = false
);
