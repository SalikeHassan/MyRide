using Common.Domain;

namespace Common.Application;

public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    Task Handle(TCommand command);
}
