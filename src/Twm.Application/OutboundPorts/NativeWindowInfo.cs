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
    // (their cloak becomes DWM_CLOAKED_SHELL). Read-only; only the Win32
    // backend sets it.
    WindowId? Owner = null,
    // WS_EX_DLGMODALFRAME: thin/double dialog border. Komorebi treats this as
    // ineligible for tiling; twm currently does not. Surface it so the
    // divergence is diagnosable in `twm --dump`. Default false for test fakes.
    bool IsDlgModalFrame = false,
    // The owning process's PID. 0 when the Win32 backend couldn't resolve it.
    int ProcessId = 0,
    // The owning process's exe basename (e.g. "KeePass.exe"), or null when
    // the backend couldn't open the process (elevated, etc.). Default null
    // for test fakes; the Win32 backend fills it via QueryFullProcessImageName.
    string? ProcessName = null,
    // Owner's window class name, or null when there's no owner or the Win32
    // backend couldn't resolve it. Read by `twm --dump` to correlate
    // owner/owned pairs at a glance.
    string? OwnerClass = null,
    // Owner's exe basename, or null when there's no owner or the backend
    // couldn't resolve it. Companion to OwnerClass.
    string? OwnerProcessName = null
);
