namespace Application;

public interface ICommandHandler<in TCommand> where TCommand : ICommandRequest
{   
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommandRequest<TResponse>
{   
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}

