using Twm.Core.Bussing;

namespace Twm.Platform.Ipc;

/// <summary>
/// A parsed <c>twm-msg</c> request. Either runs a core command through the bus,
/// queries the tree, or performs an app-level action (close/exit). Mirrors
/// <c>KeyEffect</c> but adds the <see cref="GetTreeRequest" /> query, which
/// keybindings have no equivalent for.
/// </summary>
public abstract record IpcRequest;

/// <summary>
/// Runs a core command through the bus (focus/move/resize/split/toggle-split/
/// workspace).
/// </summary>
public sealed record RunCommandRequest(ICommand Command) : IpcRequest;

/// <summary>Return the current layout tree as JSON (<c>get-tree</c>).</summary>
public sealed record GetTreeRequest : IpcRequest;

/// <summary>Close the focused window.</summary>
public sealed record CloseRequest : IpcRequest;

/// <summary>Exit the window manager.</summary>
public sealed record ExitRequest : IpcRequest;
