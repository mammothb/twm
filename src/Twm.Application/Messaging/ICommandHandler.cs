namespace Twm.Application.Messaging;

/// <summary>Handles a single command types.</summary>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    CommandResult Handle(TCommand command);
}
