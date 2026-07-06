namespace Api;

public interface IHandler
{
    Task<Result> SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) 
        where TCommand : ICommandRequest;

    Task<Result<TResponse>> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default) 
        where TCommand : ICommandRequest<TResponse>;

    Task<Result<TResponse>> QueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default) 
        where TQuery : IQueryRequest<TResponse>;
}


