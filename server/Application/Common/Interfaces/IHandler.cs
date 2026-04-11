using Application.Common.Results;
using Application.Common.Interfaces;

namespace Application.Common.Interfaces;

public interface IHandler
{
    Task<Result> SendAsync<TCommand>(TCommand command, CancellationToken ct = default) 
        where TCommand : ICommandRequest;

    Task<Result<TResponse>> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken ct = default) 
        where TCommand : ICommandRequest<TResponse>;

    Task<Result<TResponse>> QueryAsync<TQuery, TResponse>(TQuery query, CancellationToken ct = default) 
        where TQuery : IQueryRequest<TResponse>;
}
