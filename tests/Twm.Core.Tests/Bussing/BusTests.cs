using Twm.Core.Bussing;

namespace Twm.Core.Tests.Bussing;

public class BusTests
{
    private sealed record IncrementCommand(int By) : ICommand;

    private sealed record ResetCommand : ICommand;

    private sealed record CounterChanged(int Value) : IEvent;

    private sealed record UnrelatedEvent : IEvent;

    private sealed class DelegateHandler<TCommand>(Func<TCommand, CommandResult> handle)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly Func<TCommand, CommandResult> _handle = handle;

        public CommandResult Handle(TCommand command)
        {
            return _handle(command);
        }
    }

    [Fact]
    public void Invoke_DispatchesToHandlerAndReturnsResult()
    {
        var bus = new Bus();
        bus.Register(new DelegateHandler<IncrementCommand>(_ => CommandResult.Ok));

        CommandResult result = bus.Invoke(new IncrementCommand(1));

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public void Invoke_PassesTheCommandInstanceToTheHandler()
    {
        var bus = new Bus();
        int seen = 0;
        bus.Register(
            new DelegateHandler<IncrementCommand>(command =>
            {
                seen = command.By;
                return CommandResult.Ok;
            })
        );

        int expected = 42;
        bus.Invoke(new IncrementCommand(expected));

        seen.ShouldBe(expected);
    }

    [Fact]
    public void Invoke_RoutesEachCommandTypeToItsOwnHandler()
    {
        var bus = new Bus();
        bool incrementCalled = false;
        bool resetCalled = false;
        bus.Register(
            new DelegateHandler<IncrementCommand>(command =>
            {
                incrementCalled = true;
                return CommandResult.Ok;
            })
        );
        bus.Register(
            new DelegateHandler<ResetCommand>(command =>
            {
                resetCalled = true;
                return CommandResult.Ok;
            })
        );

        bus.Invoke(new ResetCommand());

        incrementCalled.ShouldBeFalse();
        resetCalled.ShouldBeTrue();
    }

    [Fact]
    public void Invoke_ReturnsFailureResultFromHandler()
    {
        var bus = new Bus();
        string message = "failed";
        bus.Register(new DelegateHandler<ResetCommand>(_ => CommandResult.Fail(message)));

        CommandResult result = bus.Invoke(new ResetCommand());

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe(message);
    }

    [Fact]
    public void Invoke_ThrowsForUnregisteredCommand()
    {
        var bus = new Bus();

        Should
            .Throw<InvalidOperationException>(() => bus.Invoke(new ResetCommand()))
            .Message.ShouldContain(nameof(ResetCommand));
    }

    [Fact]
    public void Register_RejectsDuplicateHandler()
    {
        var bus = new Bus();
        bus.Register(new DelegateHandler<ResetCommand>(_ => CommandResult.Ok));

        Should.Throw<InvalidOperationException>(() =>
            bus.Register(new DelegateHandler<ResetCommand>(_ => CommandResult.Ok))
        );
    }

    [Fact]
    public void Emit_FansOutToAllSubscribersOfThatType()
    {
        var bus = new Bus();
        int first = 0;
        int second = 0;
        bus.Subscribe<CounterChanged>(e => first = e.Value);
        bus.Subscribe<CounterChanged>(e => second = e.Value);

        int expected = 7;
        bus.Emit(new CounterChanged(expected));

        first.ShouldBe(expected);
        second.ShouldBe(expected);
    }

    [Fact]
    public void Emit_OnlyNotifiesSubscribersOfTheMatchingType()
    {
        var bus = new Bus();
        bool counterSeen = false;
        bool unrelatedSeen = false;
        bus.Subscribe<CounterChanged>(_ => counterSeen = true);
        bus.Subscribe<UnrelatedEvent>(_ => unrelatedSeen = true);

        bus.Emit(new CounterChanged(1));

        counterSeen.ShouldBeTrue();
        unrelatedSeen.ShouldBeFalse();
    }

    [Fact]
    public void Emit_WithNoSubscribers_IsNoOp()
    {
        var bus = new Bus();

        Should.NotThrow(() => bus.Emit(new UnrelatedEvent()));
    }

    [Fact]
    public void CommandHistory_RecordsInvocationsInOrder()
    {
        var bus = new Bus();
        bus.Register(new DelegateHandler<IncrementCommand>(_ => CommandResult.Ok));
        bus.Register(new DelegateHandler<ResetCommand>(_ => CommandResult.Ok));

        bus.Invoke(new IncrementCommand(1));
        bus.Invoke(new ResetCommand());
        bus.Invoke(new IncrementCommand(2));

        bus.CommandHistory.ShouldBe([
            nameof(IncrementCommand),
            nameof(ResetCommand),
            nameof(IncrementCommand),
        ]);
    }

    [Fact]
    public void CommandHistory_IsBoundedKeepingMostRecent()
    {
        int commandHistoryCapacity = 2;
        var bus = new Bus(commandHistoryCapacity);
        bus.Register(new DelegateHandler<IncrementCommand>(_ => CommandResult.Ok));
        bus.Register(new DelegateHandler<ResetCommand>(_ => CommandResult.Ok));

        bus.Invoke(new IncrementCommand(1));
        bus.Invoke(new ResetCommand());
        bus.Invoke(new IncrementCommand(2));

        bus.CommandHistory.ShouldBe([nameof(ResetCommand), nameof(IncrementCommand)]);
        bus.CommandHistory.Count.ShouldBe(commandHistoryCapacity);
    }
}
