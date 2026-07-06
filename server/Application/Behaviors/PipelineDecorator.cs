using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;
using System.Text.Json;

namespace Application;
public static class PipelineDecorator
{
    internal class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> inner,
        ILogger<ICommandHandler<TCommand, TResponse>> logger,
        IServiceProvider serviceProvider) : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommandRequest<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var requestName = typeof(TCommand).Name;
            
            var currentUserService = serviceProvider.GetRequiredService<CurrentUserService>();
            var workspaceContext = serviceProvider.GetRequiredService<WorkspaceContext>();
            
            var userId = currentUserService.CurrentUserId();
            var workspaceIdResult = workspaceContext.TryGetWorkspaceId();
            var workspaceId = workspaceIdResult.IsSuccess ? workspaceIdResult.Value : (Guid?)null;
            var flowId = Guid.NewGuid().ToString("N")[..8];

            using (LogContext.PushProperty("FlowId", flowId))
            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("WorkspaceId", workspaceId))
            {
                var payload = JsonSerializer.Serialize(command);
                var sw = Stopwatch.StartNew();

                try
                {
                    // 2. Lazy Validation
                    var validators = serviceProvider.GetRequiredService<IEnumerable<IValidator<TCommand>>>();
                    if (validators.Any())
                    {
                        var context = new ValidationContext<TCommand>(command);
                        var failures = validators.Select(v => v.Validate(context)).SelectMany(r => r.Errors).Where(f => f != null).ToList();
                        if (failures.Count > 0) return Result<TResponse>.Failure(Error.Validation("Request.ValidationFailed", string.Join(" | ", failures.Select(f => f.ErrorMessage))));
                    }

                    // 3. Authorization
                    if (command is IAuthorizedWorkspaceRequest)
                    {
                        var authResult = AuthorizeInternal(workspaceContext, workspaceId);
                        if (authResult is not null)
                        {
                            logger.LogWarning("Command {RequestName} failed authorization: {Error}", requestName, authResult.Error);
                            return Result<TResponse>.Failure(authResult.Error!);
                        }
                    }

                    // 4. Execution with Transaction Safety
                    // Automatically wraps handlers in Execution Strategy so retries and connection pools don't break transactions
                    var db = serviceProvider.GetRequiredService<TaskPlanDbContext>();
                    var strategy = db.Database.CreateExecutionStrategy();
                    
                    var result = await strategy.ExecuteAsync(async () => 
                    {
                        return await inner.Handle(command, cancellationToken);
                    });

                    sw.Stop();

                    if (result.IsSuccess)
                        logger.LogInformation("Command {RequestName} executed successfully in {ElapsedMilliseconds}ms", requestName, sw.ElapsedMilliseconds);
                    else
                        logger.LogWarning("Command {RequestName} failed gracefully in {ElapsedMilliseconds}ms: {Error}", requestName, sw.ElapsedMilliseconds, result.Error);

                    return result;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    logger.LogError(ex, "Unhandled exception in command {RequestName} after {ElapsedMilliseconds}ms", requestName, sw.ElapsedMilliseconds);
                    throw;
                }
            }
        }
    }

    internal class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> inner,
        ILogger<ICommandHandler<TCommand>> logger,
        IServiceProvider serviceProvider) : ICommandHandler<TCommand>
        where TCommand : ICommandRequest
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var requestName = typeof(TCommand).Name;
            var currentUserService = serviceProvider.GetRequiredService<CurrentUserService>();
            var workspaceContext = serviceProvider.GetRequiredService<WorkspaceContext>();
            
            var userId = currentUserService.CurrentUserId();
            var workspaceIdResult = workspaceContext.TryGetWorkspaceId();
            var workspaceId = workspaceIdResult.IsSuccess ? workspaceIdResult.Value : (Guid?)null;
            var flowId = Guid.NewGuid().ToString("N")[..8];

            using (LogContext.PushProperty("FlowId", flowId))
            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("WorkspaceId", workspaceId))
            {
                var payload = JsonSerializer.Serialize(command);
                var sw = Stopwatch.StartNew();

                try
                {
                    var validators = serviceProvider.GetRequiredService<IEnumerable<IValidator<TCommand>>>();
                    if (validators.Any())
                    {
                        var context = new ValidationContext<TCommand>(command);
                        var failures = validators.Select(v => v.Validate(context)).SelectMany(r => r.Errors).Where(f => f != null).ToList();
                        if (failures.Count > 0) return Result.Failure(Error.Validation("Request.ValidationFailed", string.Join(" | ", failures.Select(f => f.ErrorMessage))));
                    }

                    if (command is IAuthorizedWorkspaceRequest)
                    {
                        var authResult = AuthorizeInternal(workspaceContext, workspaceId);
                        if (authResult is not null)
                        {
                            logger.LogWarning("Command {RequestName} failed authorization: {Error}", requestName, authResult.Error);
                            return authResult;
                        }
                    }

                    // 4. Execution with Transaction Safety
                    var db = serviceProvider.GetRequiredService<TaskPlanDbContext>();
                    var strategy = db.Database.CreateExecutionStrategy();
                    
                    var result = await strategy.ExecuteAsync(async () => 
                    {
                        return await inner.Handle(command, cancellationToken);
                    });
                    
                    sw.Stop();

                    if (result.IsSuccess)
                        logger.LogInformation("Command {RequestName} executed successfully in {ElapsedMilliseconds}ms", requestName, sw.ElapsedMilliseconds);
                    else
                        logger.LogWarning("Command {RequestName} failed gracefully in {ElapsedMilliseconds}ms: {Error}", requestName, sw.ElapsedMilliseconds, result.Error);

                    return result;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    logger.LogError(ex, "Unhandled exception in command {RequestName} after {ElapsedMilliseconds}ms", requestName, sw.ElapsedMilliseconds);
                    throw;
                }
            }
        }
    }

    internal class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> inner,
        ILogger<IQueryHandler<TQuery, TResponse>> logger,
        IServiceProvider serviceProvider) : IQueryHandler<TQuery, TResponse>
        where TQuery : IQueryRequest<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            var requestName = typeof(TQuery).Name;
            var currentUserService = serviceProvider.GetRequiredService<CurrentUserService>();
            var workspaceContext = serviceProvider.GetRequiredService<WorkspaceContext>();
            
            var userId = currentUserService.CurrentUserId();
            var workspaceIdResult = workspaceContext.TryGetWorkspaceId();
            var workspaceId = workspaceIdResult.IsSuccess ? workspaceIdResult.Value : (Guid?)null;
            var flowId = Guid.NewGuid().ToString("N")[..8];

            using (LogContext.PushProperty("FlowId", flowId))
            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("WorkspaceId", workspaceId))
            {
                logger.LogInformation("Executing query {RequestName}", requestName);
                var sw = Stopwatch.StartNew();

                try
                {
                    var validators = serviceProvider.GetRequiredService<IEnumerable<IValidator<TQuery>>>();
                    if (validators.Any())
                    {
                        var context = new ValidationContext<TQuery>(query);
                        var failures = validators.Select(v => v.Validate(context)).SelectMany(r => r.Errors).Where(f => f != null).ToList();
                        if (failures.Count > 0) return Result<TResponse>.Failure(Error.Validation("Request.ValidationFailed", string.Join(" | ", failures.Select(f => f.ErrorMessage))));
                    }

                    if (query is IAuthorizedWorkspaceRequest)
                    {
                        var authResult = AuthorizeInternal(workspaceContext, workspaceId);
                        if (authResult is not null) return Result<TResponse>.Failure(authResult.Error!);
                    }

                    var result = await inner.Handle(query, cancellationToken);
                    sw.Stop();

                    if (result.IsSuccess)
                        logger.LogInformation("Query {RequestName} executed successfully in {ElapsedMilliseconds}ms", requestName, sw.ElapsedMilliseconds);
                    else
                        logger.LogError("Query {RequestName} failed in {ElapsedMilliseconds}ms: {Error}", requestName, sw.ElapsedMilliseconds, result.Error);

                    return result;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    logger.LogError(ex, "Unhandled exception in query {RequestName} after {ElapsedMilliseconds}ms", requestName, sw.ElapsedMilliseconds);
                    throw;
                }
            }
        }
    }

    private static Result? AuthorizeInternal(WorkspaceContext workspaceContext, Guid? workspaceId)
    {
        if (!workspaceId.HasValue || !workspaceContext.HasCurrentMember)
            return Result.Failure(Error.Validation("Workspace.Required", "Workspace context is required for this request."));

        return null;
    }
}






