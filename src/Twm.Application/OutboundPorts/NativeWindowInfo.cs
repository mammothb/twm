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
    bool HasWindowEdge = true
);
