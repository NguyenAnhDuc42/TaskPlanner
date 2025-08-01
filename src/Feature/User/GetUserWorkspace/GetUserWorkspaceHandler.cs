using System;
using System.Data;
using Dapper;
using MediatR;
using src.Contract;
using src.Domain.Enums;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IServices;

namespace src.Feature.User.GetUserWorkspace;

public class GetUserWorkspaceHandler : IRequestHandler<GetUserWorkspaceRequest, Result<List<WorkspaceDetail>, ErrorResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDbConnection _dbConnection;

    private record WorkspaceQueryResult(
        Guid Id,
        string Name,
        string Description,
        string Color,
        DateTime CreatedAtUtc,
        Role YourRole,
        Guid OwnerId,
        string OwnerFullName,
        string OwnerEmail,
        Role OwnerRole,
        int MemberCount,
        string? JoinCode
    );

    public GetUserWorkspaceHandler(ICurrentUserService currentUserService, IDbConnection dbConnection)
    {
        _currentUserService = currentUserService;
        _dbConnection = dbConnection;
    }

    public async Task<Result<List<WorkspaceDetail>, ErrorResponse>> Handle(GetUserWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        const string query =
        """ 
            SELECT
                w."Id",
                w."Name",
                w."Description",
                w."Color",
                w."CreatedAtUtc",
                uw."Role" AS "YourRole",
                owner."Id" AS "OwnerId",
                owner."FullName" AS "OwnerFullName",
                owner."Email" AS "OwnerEmail",
                owner."Role" AS "OwnerRole",
                (SELECT COUNT(*) FROM "UserWorkspaces" WHERE "WorkspaceId" = w."Id") AS "MemberCount",
                CASE 
                    WHEN uw."Role" = 0 THEN w."JoinCode"
                    ELSE NULL 
                END AS "JoinCode"
            FROM "Workspaces" AS w
            INNER JOIN "UserWorkspaces" AS uw ON w."Id" = uw."WorkspaceId"
            INNER JOIN "Users" AS owner ON w."CreatorId" = owner."Id"
            WHERE uw."UserId" = @CurrentUserId
            ORDER BY w."Name";
        """;

        var queryResults = await _dbConnection.QueryAsync<WorkspaceQueryResult>(
            query, 
            new { CurrentUserId = currentUserId }
        );

        var workspaces = queryResults.Select(item => new WorkspaceDetail(
            item.Id,
            item.Name,
            item.Description,
            item.Color,
            item.YourRole,
            new UserSummary(
                item.OwnerId,
                item.OwnerFullName,
                item.OwnerEmail,
                item.OwnerRole
            ),
            item.MemberCount,
            item.CreatedAtUtc,
            item.JoinCode,
            null, // Members list is null for the dashboard view
            null  // Spaces list is null for the dashboard view
        )).ToList();

        return Result<List<WorkspaceDetail>, ErrorResponse>.Success(workspaces);
    }
}