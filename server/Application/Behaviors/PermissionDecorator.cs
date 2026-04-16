using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Common.Errors;
using Application.Features;
using Application.Helpers;
using Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Behaviors;

public static class PermissionDecorator
{
    private static async Task<Result?> AuthorizeAsync<TRequest>(TRequest request, WorkspaceContext workspaceContext, ICurrentUserService currentUserService, IDataBase db, CancellationToken ct)
    {
        if (request is not IAuthorizedWorkspaceRequest)
            return null;

        var workspaceIdResult = workspaceContext.TryGetWorkspaceId();
        if (workspaceIdResult.IsFailure) return Result.Failure(workspaceIdResult.Error!);

        var userId = currentUserService.CurrentUserId();
        var workspaceId = workspaceIdResult.Value;

        if (workspaceContext.CurrentMember != null && workspaceContext.CurrentMember.UserId == userId)
            return null;

        var member = await db.WorkspaceMembers
            .AsNoTracking()
            .ByMember(workspaceId, userId)
            .FirstOrDefaultAsync(ct);

        if (member == null)
            return Result.Failure(MemberError.DontHavePermission);

        workspaceContext.CurrentMember = member;
        return null;
    }

    internal class QueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
        where TQuery : IQueryRequest<TResponse>
    {
        private readonly IQueryHandler<TQuery, TResponse> _inner;
        private readonly WorkspaceContext _workspaceContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDataBase _db;

        public QueryHandler(IQueryHandler<TQuery, TResponse> inner, WorkspaceContext workspaceContext, ICurrentUserService currentUserService, IDataBase db)
        {
            _inner = inner;
            _workspaceContext = workspaceContext;
            _currentUserService = currentUserService;
            _db = db;
        }

        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken ct)
        {
            var authResult = await AuthorizeAsync(query, _workspaceContext, _currentUserService, _db, ct);
            if (authResult is not null) return Result<TResponse>.Failure(authResult.Error!);

            return await _inner.Handle(query, ct);
        }
    }

    internal class CommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommandRequest<TResponse>
    {
        private readonly ICommandHandler<TCommand, TResponse> _inner;
        private readonly WorkspaceContext _workspaceContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDataBase _db;

        public CommandHandler(ICommandHandler<TCommand, TResponse> inner, WorkspaceContext workspaceContext, ICurrentUserService currentUserService, IDataBase db)
        {
            _inner = inner;
            _workspaceContext = workspaceContext;
            _currentUserService = currentUserService;
            _db = db;
        }

        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken ct)
        {
            var authResult = await AuthorizeAsync(command, _workspaceContext, _currentUserService, _db, ct);
            if (authResult is not null) return Result<TResponse>.Failure(authResult.Error!);

            return await _inner.Handle(command, ct);
        }
    }

    internal class CommandBaseHandler<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommandRequest
    {
        private readonly ICommandHandler<TCommand> _inner;
        private readonly WorkspaceContext _workspaceContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDataBase _db;

        public CommandBaseHandler(ICommandHandler<TCommand> inner, WorkspaceContext workspaceContext, ICurrentUserService currentUserService, IDataBase db)
        {
            _inner = inner;
            _workspaceContext = workspaceContext;
            _currentUserService = currentUserService;
            _db = db;
        }

        public async Task<Result> Handle(TCommand command, CancellationToken ct)
        {
            var authResult = await AuthorizeAsync(command, _workspaceContext, _currentUserService, _db, ct);
            if (authResult is not null) return authResult;

            return await _inner.Handle(command, ct);
        }
    }
}
