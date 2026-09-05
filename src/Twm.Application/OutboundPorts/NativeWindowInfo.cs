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
    // Diagnostics for evaluating komorebi's allowlist criteria (require
    // WS_CAPTION|WS_EX_WINDOWEDGE). Default true so fakes/tests read as normal
    // windows; only the Win32 backend sets them per-window
    bool HasCaption = true,
    bool HasWindowEdge = true,
    // The HWND of this window's owner (GW_OWNER), or null. Owned windows
    // (modal dialogs, popups) are hidden by DWM when their owner is cloaked
    // (DWM_CLOAKED_INHERITED). Read-only; only the Win32 backend sets it.
    WindowId? Owner = null,
    // Full DWMWA_CLOAKED value: 0 uncloaked, 1 cloaked by the app, 2 cloaked
    // by the shell, 4 inherited from a cloaked owner. Diagnostic: the full
    // value distinguishes a cascade from Twm's own cloak, which IsCloaked
    // collapses to a bool. Read-only; only the Win32 backend sets it.
    int CloakValue = 0
);
