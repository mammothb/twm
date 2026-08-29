using Twm.Core.Geometry;

namespace Twm.Platform;

/// <summary>
/// Opaque identifier for a display. The Win32 layer maps this to an
/// <c>HMONITOR</c>; the platform-neutral code never interprets the value.
/// </summary>
public readonly record struct MonitorId(nint Value);

/// <summary>
/// A snapshot of one display as reported by the OS at enumeration time.
/// <see cref="Bounds" /> is the full monitor rectangle in virtual-screen
/// coordinates; <see cref="WorkArea" /> excludes the taskbar/appbars and is
/// what windows tile within.
/// </summary>
public sealed record MonitorInfo(MonitorId Id, Rect Bounds, Rect WorkArea, bool IsPrimary);
