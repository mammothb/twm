using Twm.Application.Messaging;

namespace Twm.Application.Coordination;

/// <summary>
/// Fired whenever the visible layout may have changed, after every reconcile
/// (adopt/remove/execute/workspace-switch) and on focus changes. A coarse
/// "re-render" signal; consumers like the status bar rebuild their view.
/// </summary>
public sealed record LayoutChangedEvent : IEvent;
