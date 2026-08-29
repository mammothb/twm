namespace Twm.Core.Bussing;

/// <summary>
/// Routes commands to a single handler each and fans events out to subscribers.
/// Registration is explicit and reflection-free: no assembly scanning and no DI
/// container. Not thread-safe; intended to be driven from one thread.
/// </summary>
public sealed class Bus
{
    private const int DefaultCommandHistoryCapacity = 100;

    private readonly Dictionary<Type, Func<ICommand, CommandResult>> _handlers = [];
    private readonly Dictionary<Type, List<Action<IEvent>>> _subscribers = [];
    private readonly Queue<string> _commandHistory = [];
    private readonly int _commandHistoryCapacity;

    public Bus()
        : this(DefaultCommandHistoryCapacity) { }

    public Bus(int commandHistoryCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(commandHistoryCapacity);
        _commandHistoryCapacity = commandHistoryCapacity;
    }

    /// <summary>
    /// Command type names in invocation order, oldest first, bounded in size.
    /// </summary>
    public IReadOnlyCollection<string> CommandHistory => _commandHistory.ToArray();

    /// <summary>
    /// Registers the single handler for <typeparamref name="TCommand" />.
    /// </summary>
    public void Register<TCommand>(ICommandHandler<TCommand> handler)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(handler);
        Type type = typeof(TCommand);
        if (_handlers.ContainsKey(type))
        {
            throw new InvalidOperationException(
                $"A handler is already registered for command '{type.Name}'."
            );
        }

        _handlers[type] = command => handler.Handle((TCommand)command);
    }

    /// <summary>
    /// Dispatches a command to its handler and returns the result.
    /// </summary>
    public CommandResult Invoke(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Type type = command.GetType();
        if (!_handlers.TryGetValue(type, out Func<ICommand, CommandResult>? handler))
        {
            throw new InvalidOperationException($"No handler registred for command '{type.Name}'.");
        }

        RecordInvocation(type);
        return handler(command);
    }

    /// <summary>
    /// Subscribes to events of type <typeparamref name="TEvent" />.
    /// </summary>
    public void Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);
        Type type = typeof(TEvent);
        if (!_subscribers.TryGetValue(type, out List<Action<IEvent>>? list))
        {
            list = [];
            _subscribers[type] = list;
        }

        list.Add(@event => handler((TEvent)@event));
    }

    /// <summary>
    /// Fans an event out to every subscriber of its exact type.
    /// </summary>
    public void Emit(IEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        if (_subscribers.TryGetValue(@event.GetType(), out List<Action<IEvent>>? list))
        {
            foreach (Action<IEvent> subscriber in list.ToArray())
            {
                subscriber(@event);
            }
        }
    }

    private void RecordInvocation(Type commandType)
    {
        _commandHistory.Enqueue(commandType.Name);
        while (_commandHistory.Count > _commandHistoryCapacity)
        {
            _commandHistory.Dequeue();
        }
    }
}
