
using System.Data;
using System.Security.Claims;
using Dapper;
using MediatR;

using src.Helper.Results;

namespace src.Feature.Workspace.SidebarWorkspaces;

public class SidebarWorkspacesHandler : IRequestHandler<SidebarWorkspacesRequest, Result<SidebarWorkspacesResponse, ErrorResponse>>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDbConnection _dbConnection;
    public SidebarWorkspacesHandler(IHttpContextAccessor httpContextAccessor, IDbConnection dbConnection)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
    }
    public async Task<Result<SidebarWorkspacesResponse, ErrorResponse>> Handle(SidebarWorkspacesRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Result<SidebarWorkspacesResponse, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Unauthorized", "User not found."));
        }

        var sql = @"SELECT w.""Id"", w.""Name"", w.""Icon""
                    FROM  ""UserWorkspaces"" uw
                    JOIN ""Workspaces"" w on uw.""WorkspaceId"" = w.""Id""
                    WHERE uw.""UserId"" = @UserId";
        try
        {
            // Dapper will map the selected columns directly to the SidebarWorkspace record
            var sidebarWorkspaces = await _dbConnection.QueryAsync<SidebarWorkspace>(
                sql,
                new { UserId = userId }
            );

            return Result<SidebarWorkspacesResponse, ErrorResponse>.Success(new SidebarWorkspacesResponse(sidebarWorkspaces.ToList()));
        }
        catch (Exception ex)
        {
            return Result<SidebarWorkspacesResponse, ErrorResponse>.Failure(ErrorResponse.Internal(ex.Message));
        }

    }
}
