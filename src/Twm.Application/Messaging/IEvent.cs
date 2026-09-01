namespace Twm.Application.Messaging;

/// <summary>
/// Marker for an event: a notification that something happened, fanned out
/// through the <see cref="Bus" /> to any number of subscribers.
/// </summary>
public interface IEvent { }
