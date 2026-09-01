namespace Twm.Application.Messaging;

/// <summary>
/// Marker for a command: an intent to change tree state, dispatched through the
/// <see cref="Bus" /> to a single registered handler.
/// </summary>
public interface ICommand { }
