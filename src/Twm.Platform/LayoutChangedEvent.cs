using Twm.Core.Bussing;

namespace Twm.Platform;

/// <summary>
/// Fired whenever the layout the user can see may have changed, after every
/// reconcile (adopt/remove/execute/workspace-switch) and on focus changes. A
/// single coarse "re-render" signal; in-process consumers like the status bar
/// subscribe and rebuild their view.
/// </summary>
public sealed record LayoutChangedEvent : IEvent;
