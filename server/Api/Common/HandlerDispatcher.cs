using Microsoft.Extensions.DependencyInjection;

namespace Api;

public class HandlerDispatcher : IHandler
{
    private readonly IServiceProvider _serviceProvider;

    public HandlerDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result> SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) 
        where TCommand : ICommandRequest
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
        return await handler.Handle(command, cancellationToken);
    }

    public async Task<Result<TResponse>> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default) 
        where TCommand : ICommandRequest<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return await handler.Handle(command, cancellationToken);
    }

    public async Task<Result<TResponse>> QueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default) 
        where TQuery : IQueryRequest<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return await handler.Handle(query, cancellationToken);
    }

}


