using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Helpers;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace Application.Behaviors;

public static class LoggingDecorator
{
    internal class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> inner, 
        ILogger<IQueryHandler<TQuery, TResponse>> logger,
        WorkspaceContext context) : IQueryHandler<TQuery, TResponse> 
        where TQuery : IQueryRequest<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TQuery request, CancellationToken cancellationToken)
        {
            var requestName = typeof(TQuery).Name;
            var userId = context.CurrentMember?.UserId;
            var workspaceId = context.TryGetWorkspaceId().IsSuccess ? context.workspaceId : (Guid?)null;

            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("WorkspaceId", workspaceId))
            {
                logger.LogInformation("Executing query {RequestName}", requestName);
                
                var sw = Stopwatch.StartNew();
                var result = await inner.Handle(request, cancellationToken);
                sw.Stop();

                if (result.IsSuccess)
                {
                    logger.LogInformation("Query {RequestName} executed successfully in {ElapsedMilliseconds}ms", 
                        requestName, sw.ElapsedMilliseconds);
                }
                else
                {
                    using (LogContext.PushProperty("Error", result.Error, true))
                    {
                        logger.LogError("Query {RequestName} failed in {ElapsedMilliseconds}ms", 
                            requestName, sw.ElapsedMilliseconds);
                    }
                }
                return result;
            }
        }
    }

    internal class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> inner, 
        ILogger<ICommandHandler<TCommand, TResponse>> logger,
        WorkspaceContext context) : ICommandHandler<TCommand, TResponse> 
        where TCommand : ICommandRequest<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand request, CancellationToken cancellationToken)
        {
            var requestName = typeof(TCommand).Name;
            var userId = context.CurrentMember?.UserId;
            var workspaceId = context.TryGetWorkspaceId().IsSuccess ? context.workspaceId : (Guid?)null;

            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("WorkspaceId", workspaceId))
            {
                logger.LogInformation("Executing command {RequestName}", requestName);
                
                var sw = Stopwatch.StartNew();
                var result = await inner.Handle(request, cancellationToken);
                sw.Stop();

                if (result.IsSuccess)
                {
                    logger.LogInformation("Command {RequestName} executed successfully in {ElapsedMilliseconds}ms", 
                        requestName, sw.ElapsedMilliseconds);
                }
                else
                {
                    using (LogContext.PushProperty("Error", result.Error, true))
                    {
                        logger.LogError("Command {RequestName} failed in {ElapsedMilliseconds}ms", 
                            requestName, sw.ElapsedMilliseconds);
                    }
                }
                return result;
            }
        }
    }

    internal class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> inner, 
        ILogger<ICommandHandler<TCommand>> logger,
        WorkspaceContext context) : ICommandHandler<TCommand> 
        where TCommand : ICommandRequest
    {
        public async Task<Result> Handle(TCommand request, CancellationToken cancellationToken)
        {
            var requestName = typeof(TCommand).Name;
            var userId = context.CurrentMember?.UserId;
            var workspaceId = context.TryGetWorkspaceId().IsSuccess ? context.workspaceId : (Guid?)null;

            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("WorkspaceId", workspaceId))
            {
                logger.LogInformation("Executing command {RequestName}", requestName);
                
                var sw = Stopwatch.StartNew();
                var result = await inner.Handle(request, cancellationToken);
                sw.Stop();

                if (result.IsSuccess)
                {
                    logger.LogInformation("Command {RequestName} executed successfully in {ElapsedMilliseconds}ms", 
                        requestName, sw.ElapsedMilliseconds);
                }
                else
                {
                    using (LogContext.PushProperty("Error", result.Error, true))
                    {
                        logger.LogError("Command {RequestName} failed in {ElapsedMilliseconds}ms", 
                            requestName, sw.ElapsedMilliseconds);
                    }
                }
                return result;
            }
        }
    }
}