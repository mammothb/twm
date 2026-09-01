using Twm.Application.Messaging;

namespace Twm.Application.Commands;

/// <summary>
/// A parsed inbound request, from a keybinding action or the <c>twm-msg</c>
/// text protocol. Either runs a core command through the bus,
/// queries the tree, or performs an app-level action (close/exit). Mirrors
/// <c>KeyEffect</c> but adds the <see cref="GetTreeRequest" /> query, which
/// keybindings have no equivalent for.
/// </summary>
public abstract record WmRequest;

/// <summary>
/// Runs a core command through the bus (focus/move/resize/split/toggle-split/
/// workspace).
/// </summary>
public sealed record RunCommandRequest(ICommand Command) : WmRequest;

/// <summary>Return the current layout tree as JSON (<c>get-tree</c>).</summary>
public sealed record GetTreeRequest : WmRequest;

/// <summary>Close the focused window.</summary>
public sealed record CloseRequest : WmRequest;

/// <summary>Exit the window manager.</summary>
public sealed record ExitRequest : WmRequest;
