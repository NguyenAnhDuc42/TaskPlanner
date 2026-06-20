using Microsoft.EntityFrameworkCore;
namespace Application;

public static class PermissionDecorator
{
    private static async Task<Result?> AuthorizeAsync<TRequest>(TRequest request, WorkspaceContext workspaceContext, CurrentUserService currentUserService, TaskPlanDbContext db, CancellationToken cancellationToken)
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
            .Where(m => m.ProjectWorkspaceId == workspaceId && m.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

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
        private readonly CurrentUserService _currentUserService;
        private readonly TaskPlanDbContext _db;

        public QueryHandler(IQueryHandler<TQuery, TResponse> inner, WorkspaceContext workspaceContext, CurrentUserService currentUserService, TaskPlanDbContext db)
        {
            _inner = inner;
            _workspaceContext = workspaceContext;
            _currentUserService = currentUserService;
            _db = db;
        }

        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            var authResult = await AuthorizeAsync(query, _workspaceContext, _currentUserService, _db, cancellationToken);
            if (authResult is not null) return Result<TResponse>.Failure(authResult.Error!);

            return await _inner.Handle(query, cancellationToken);
        }
    }

    internal class CommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommandRequest<TResponse>
    {
        private readonly ICommandHandler<TCommand, TResponse> _inner;
        private readonly WorkspaceContext _workspaceContext;
        private readonly CurrentUserService _currentUserService;
        private readonly TaskPlanDbContext _db;

        public CommandHandler(ICommandHandler<TCommand, TResponse> inner, WorkspaceContext workspaceContext, CurrentUserService currentUserService, TaskPlanDbContext db)
        {
            _inner = inner;
            _workspaceContext = workspaceContext;
            _currentUserService = currentUserService;
            _db = db;
        }

        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var authResult = await AuthorizeAsync(command, _workspaceContext, _currentUserService, _db, cancellationToken);
            if (authResult is not null) return Result<TResponse>.Failure(authResult.Error!);

            return await _inner.Handle(command, cancellationToken);
        }
    }

    internal class CommandBaseHandler<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommandRequest
    {
        private readonly ICommandHandler<TCommand> _inner;
        private readonly WorkspaceContext _workspaceContext;
        private readonly CurrentUserService _currentUserService;
        private readonly TaskPlanDbContext _db;

        public CommandBaseHandler(ICommandHandler<TCommand> inner, WorkspaceContext workspaceContext, CurrentUserService currentUserService, TaskPlanDbContext db)
        {
            _inner = inner;
            _workspaceContext = workspaceContext;
            _currentUserService = currentUserService;
            _db = db;
        }

        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var authResult = await AuthorizeAsync(command, _workspaceContext, _currentUserService, _db, cancellationToken);
            if (authResult is not null) return authResult;

            return await _inner.Handle(command, cancellationToken);
        }
    }
}



