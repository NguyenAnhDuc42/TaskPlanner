
using System.Data;
using System.Security.Claims;
using Dapper;
using MediatR;
using src.Application.Common.DTOs;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IServices;

namespace src.Feature.WorkspaceManager.SidebarWorkspaces;

public class SidebarWorkspacesHandler : IRequestHandler<SidebarWorkspacesRequest, Result<GroupWorkspace, ErrorResponse>>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDbConnection _dbConnection;
    public SidebarWorkspacesHandler(IHttpContextAccessor httpContextAccessor, ICurrentUserService currentUserService, IDbConnection dbConnection)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }
    public async Task<Result<GroupWorkspace, ErrorResponse>> Handle(SidebarWorkspacesRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();
        if (userId == Guid.Empty)
        {
            return Result<GroupWorkspace, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Unauthorized"));
        }
        var sql = @"SELECT w.""Id"", w.""Name""
                    FROM  ""UserWorkspaces"" uw
                    JOIN ""Workspaces"" w on uw.""WorkspaceId"" = w.""Id""
                    WHERE uw.""UserId"" = @UserId";

        var sidebarWorkspaces = await _dbConnection.QueryAsync<WorkspaceSummary>(sql, new { UserId = userId });
        var currentWorkspace = CurrentWorkspace(sidebarWorkspaces, request.workspaceId);
        if (currentWorkspace == null)
        {
            return Result<GroupWorkspace, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found"));
        }
        var otherWorkspaces = OtherWorkspaces(sidebarWorkspaces, request.workspaceId);
        return Result<GroupWorkspace, ErrorResponse>.Success(new GroupWorkspace(currentWorkspace, otherWorkspaces));
    }

    private WorkspaceSummary? CurrentWorkspace(IEnumerable<WorkspaceSummary> workspaces, Guid currentWorkspaceId)
    {
        return workspaces.FirstOrDefault(w => w.Id == currentWorkspaceId);
    }
    private IEnumerable<WorkspaceSummary> OtherWorkspaces(IEnumerable<WorkspaceSummary> workspaces, Guid currentWorkspaceId)
    {
        return workspaces.Where(w => w.Id != currentWorkspaceId).ToList();
    }
}
