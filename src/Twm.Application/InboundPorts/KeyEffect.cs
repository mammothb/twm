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

/// <summary>
/// Re-read the display topology and re-tile (after a resolution/aspect change
/// or a monitor add/remove)
/// </summary>
public sealed record ReconcileDisplays : KeyEffect;

/// <summary>
/// Spawn a child process via the injected <c>IProcessLauncher</c>.
/// Mirror of <c>StartProgramRequest</c> for the keymap side: parsed from
/// the <c>exec &lt;command line&gt;</c> action string.
/// </summary>
public sealed record StartProgram(string CommandLine) : KeyEffect;
