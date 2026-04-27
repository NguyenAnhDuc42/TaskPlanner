using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Common;

public class HandlerDispatcher : IHandler
{
    private readonly IServiceProvider _serviceProvider;

    public HandlerDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result> SendAsync<TCommand>(TCommand command, CancellationToken ct = default) 
        where TCommand : ICommandRequest
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
        return await handler.Handle(command, ct);
    }

    public async Task<Result<TResponse>> SendAsync<TCommand, TResponse>(TCommand command, CancellationToken ct = default) 
        where TCommand : ICommandRequest<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return await handler.Handle(command, ct);
    }

    public async Task<Result<TResponse>> QueryAsync<TQuery, TResponse>(TQuery query, CancellationToken ct = default) 
        where TQuery : IQueryRequest<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        return await handler.Handle(query, ct);
    }

}
