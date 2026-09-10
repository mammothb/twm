using Twm.Application.Messaging;

namespace Twm.Application.Coordination;

/// <summary>
/// Fired when <see cref="WmSession.ReconcileDisplays" /> actually changed the
/// layout on a display change.
/// </summary>
public sealed record DisplaysReconciledEvent : IEvent;
