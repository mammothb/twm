using Twm.Application.Messaging;

namespace Twm.Application.InboundPorts;

/// <summary>
/// What a hotkey does. Tiling actions carry a core <see cref="ICommand" />
/// (<see cref="RunCommand" />); the rest are app-level, handled by the host
/// entrypoint. The keymap maps a <see cref="KeyBinding" /> to one of these.
/// </summary>
public abstract record KeyEffect;

/// <summary>
/// Runs a core command through the bus (focus/move/toggle/resize/workspace).
/// </summary>
public sealed record RunCommand(ICommand Command) : KeyEffect;

/// <summary>Close the focused window (posts <c>WM_CLOSE</c>).</summary>
public sealed record CloseFocusedWindow : KeyEffect;

/// <summary>Exit the window manager.</summary>
public sealed record ExitWm : KeyEffect;
