using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace Application.Behaviors;

/// <summary>
/// A high-performance consolidated pipeline decorator.
/// Uses lazy resolution (IServiceProvider) to avoid expensive DI overhead for simple requests.
/// </summary>
public static class PipelineDecorator
{
    internal class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> inner,
        ILogger<ICommandHandler<TCommand, TResponse>> logger,
        IServiceProvider serviceProvider) : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommandRequest<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct)
        {
            var requestName = typeof(TCommand).Name;
            
            // 1. Resolve basic services (fast)
            var currentUserService = serviceProvider.GetRequiredService<ICurrentUserService>();
            var workspaceContext = serviceProvider.GetRequiredService<WorkspaceContext>();
            
            var userId = currentUserService.CurrentUserId();
            var workspaceIdResult = workspaceContext.TryGetWorkspaceId();
            var workspaceId = workspaceIdResult.IsSuccess ? workspaceIdResult.Value : (Guid?)null;

            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("WorkspaceId", workspaceId))
            {
                logger.LogInformation("Executing command {RequestName}", requestName);
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

                    // 3. Lazy Authorization
                    if (command is IAuthorizedWorkspaceRequest)
                    {
                        var db = serviceProvider.GetRequiredService<IDataBase>();
                        var authResult = await AuthorizeInternalAsync(workspaceContext, userId, workspaceId, db, ct);
                        if (authResult is not null) return Result<TResponse>.Failure(authResult.Error!);
                    }

                    // 4. Execution
                    var result = await inner.Handle(command, ct);
                    sw.Stop();

                    if (result.IsSuccess)
                        logger.LogInformation("Command {RequestName} executed successfully in {ElapsedMilliseconds}ms", requestName, sw.ElapsedMilliseconds);
                    else
                        logger.LogError("Command {RequestName} failed in {ElapsedMilliseconds}ms: {Error}", requestName, sw.ElapsedMilliseconds, result.Error);

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
        public async Task<Result> Handle(TCommand command, CancellationToken ct)
        {
            var requestName = typeof(TCommand).Name;
            var currentUserService = serviceProvider.GetRequiredService<ICurrentUserService>();
            var workspaceContext = serviceProvider.GetRequiredService<WorkspaceContext>();
            
            var userId = currentUserService.CurrentUserId();
            var workspaceIdResult = workspaceContext.TryGetWorkspaceId();
            var workspaceId = workspaceIdResult.IsSuccess ? workspaceIdResult.Value : (Guid?)null;

            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("WorkspaceId", workspaceId))
            {
                logger.LogInformation("Executing command {RequestName}", requestName);
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
                        var db = serviceProvider.GetRequiredService<IDataBase>();
                        var authResult = await AuthorizeInternalAsync(workspaceContext, userId, workspaceId, db, ct);
                        if (authResult is not null) return authResult;
                    }

                    var result = await inner.Handle(command, ct);
                    sw.Stop();

                    if (result.IsSuccess)
                        logger.LogInformation("Command {RequestName} executed successfully in {ElapsedMilliseconds}ms", requestName, sw.ElapsedMilliseconds);
                    else
                        logger.LogError("Command {RequestName} failed in {ElapsedMilliseconds}ms: {Error}", requestName, sw.ElapsedMilliseconds, result.Error);

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
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken ct)
        {
            var requestName = typeof(TQuery).Name;
            var currentUserService = serviceProvider.GetRequiredService<ICurrentUserService>();
            var workspaceContext = serviceProvider.GetRequiredService<WorkspaceContext>();
            
            var userId = currentUserService.CurrentUserId();
            var workspaceIdResult = workspaceContext.TryGetWorkspaceId();
            var workspaceId = workspaceIdResult.IsSuccess ? workspaceIdResult.Value : (Guid?)null;

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
                        var db = serviceProvider.GetRequiredService<IDataBase>();
                        var authResult = await AuthorizeInternalAsync(workspaceContext, userId, workspaceId, db, ct);
                        if (authResult is not null) return Result<TResponse>.Failure(authResult.Error!);
                    }

                    var result = await inner.Handle(query, ct);
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

    private static async Task<Result?> AuthorizeInternalAsync(WorkspaceContext workspaceContext, Guid userId, Guid? workspaceId, IDataBase db, CancellationToken ct)
    {
        if (!workspaceId.HasValue) 
            return Result.Failure(Error.Validation("Workspace.Required", "Workspace context is required for this request."));

        if (workspaceContext.CurrentMember != null && workspaceContext.CurrentMember.UserId == userId)
            return null;

        var member = await db.WorkspaceMembers
            .AsNoTracking()
            .ByMember(workspaceId.Value, userId)
            .FirstOrDefaultAsync(ct);

        if (member == null)
            return Result.Failure(MemberError.DontHavePermission);

        workspaceContext.CurrentMember = member;
        return null;
    }
}
