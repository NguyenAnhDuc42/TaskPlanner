
using System;
using System.Data;
using System.Linq;
using MediatR;
using Dapper;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Contract;

namespace src.Feature.WorkspaceManager.ShowMembers;

public class ShowMembersHandler : IRequestHandler<ShowMembersRequest, Result<List<UserSummary>, ErrorResponse>>
{
    private readonly IDbConnection _dbConnection;
    private readonly IHierarchyRepository _hierarchyRepository;
    public ShowMembersHandler(IDbConnection dbConnection, IHierarchyRepository hierarchyRepository)
    {
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
    }
    public async Task<Result<List<UserSummary>, ErrorResponse>> Handle(ShowMembersRequest request, CancellationToken cancellationToken)
    {
        var wokspace = await _hierarchyRepository.GetWorkspaceByIdAsync(request.workspaceId, cancellationToken);
        if (wokspace == null)
        {
            return Result<List<UserSummary>, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found."));
        }

        var sql = @"SELECT  u.""Id"",
                            u.""Name"",
                            u.""Email"",
                            uw.""Role""
                    FROM ""Users"" AS u
                    INNER JOIN ""UserWorkspaces"" AS uw ON u.""Id"" = uw.""UserId""
                    WHERE uw.""WorkspaceId"" = @WorkspaceId";

        var members = await _dbConnection.QueryAsync<UserSummary>(sql, new { WorkspaceId = request.workspaceId });

        return Result<List<UserSummary>, ErrorResponse>.Success(members.ToList());
    }
}
