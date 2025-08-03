
using System.Data;
using System.Security.Claims;
using Dapper;
using MediatR;
using src.Contract;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager.SidebarWorkspaces;

public class SidebarWorkspacesHandler : IRequestHandler<SidebarWorkspacesRequest, Result<List<WorkspaceSummary>, ErrorResponse>>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDbConnection _dbConnection;
    public SidebarWorkspacesHandler(IHttpContextAccessor httpContextAccessor, IDbConnection dbConnection)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
    }
    public async Task<Result<List<WorkspaceSummary>, ErrorResponse>> Handle(SidebarWorkspacesRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Result<List<WorkspaceSummary>, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Unauthorized"));
        }

        var sql = @"SELECT w.""Id"", w.""Name""
                    FROM  ""UserWorkspaces"" uw
                    JOIN ""Workspaces"" w on uw.""WorkspaceId"" = w.""Id""
                    WHERE uw.""UserId"" = @UserId";

        var sidebarWorkspaces = await _dbConnection.QueryAsync<WorkspaceSummary>(sql,new { UserId = userId });
        return Result<List<WorkspaceSummary>, ErrorResponse>.Success(sidebarWorkspaces.ToList());


    }
}
