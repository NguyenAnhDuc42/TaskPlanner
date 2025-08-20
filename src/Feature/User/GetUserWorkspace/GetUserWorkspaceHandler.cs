using System;
using System.Data;
using System.Linq;
using Dapper;
using MediatR;
using src.Application.Common.DTOs;
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
        string Icon,
        DateTime CreatedAt,
        Role YourRole,
        Guid OwnerId,
        string OwnerFullName,
        string OwnerEmail,
        long MemberCount,
        string? JoinCode
    );

    // Fixed: Use int for RoleValue and convert explicitly
    private record MemberQueryResult
    {
        public Guid WorkspaceId { get; init; }
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public int RoleValue { get; init; }
        
        public UserSummary ToUserSummary() => new(Id, Name, Email, (Role)RoleValue);
    }

    public GetUserWorkspaceHandler(ICurrentUserService currentUserService, IDbConnection dbConnection)
    {
        _currentUserService = currentUserService;
        _dbConnection = dbConnection;
    }

    public async Task<Result<List<WorkspaceDetail>, ErrorResponse>> Handle(GetUserWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        
        const string workspaceQuery =
        """ 
            SELECT
                w."Id", 
                w."Name",
                w."Description",
                w."Color",
                w."Icon",
                w."CreatedAt",
                uw."Role" AS "YourRole",
                owner."Id" AS "OwnerId",
                owner."Name" AS "OwnerFullName",
                owner."Email" AS "OwnerEmail",
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

        var workspaceResults = (await _dbConnection.QueryAsync<WorkspaceQueryResult>(
            workspaceQuery, 
            new { CurrentUserId = currentUserId }
        )).ToList();

        if (!workspaceResults.Any())
        {
            return Result<List<WorkspaceDetail>, ErrorResponse>.Success(new List<WorkspaceDetail>());
        }

        var workspaceIds = workspaceResults.Select(w => w.Id).ToList();

        // Fixed: Use RoleValue alias and explicit int mapping
        const string membersQuery = 
        """
            SELECT
                uw."WorkspaceId",
                u."Id",
                u."Name",
                u."Email",
                uw."Role" as "RoleValue"
            FROM "UserWorkspaces" uw
            JOIN "Users" u ON uw."UserId" = u."Id"
            WHERE uw."WorkspaceId" = ANY(@WorkspaceIds)
            ORDER BY uw."Role", u."Name";
        """;

        var memberDtos = await _dbConnection.QueryAsync<MemberQueryResult>(
            membersQuery,
            new { WorkspaceIds = workspaceIds }
        );

        // Fixed: Use the ToUserSummary() method for explicit conversion
        var membersByWorkspaceId = memberDtos
            .GroupBy(m => m.WorkspaceId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.ToUserSummary()).ToList());

        var workspaces = workspaceResults.Select(item => new WorkspaceDetail(
            item.Id,
            item.Name,
            item.Description,
            item.Color,
            item.Icon,
            item.YourRole,
            new UserSummary(
                item.OwnerId,
                item.OwnerFullName,
                item.OwnerEmail,
                Role.Owner
            ),
            (int)item.MemberCount,
            item.CreatedAt,
            item.JoinCode,
            membersByWorkspaceId.GetValueOrDefault(item.Id, new List<UserSummary>()),
            null
        )).ToList();

        return Result<List<WorkspaceDetail>, ErrorResponse>.Success(workspaces);
    }
}