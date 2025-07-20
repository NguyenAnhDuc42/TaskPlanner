
using System;
using System.Data;
using System.Linq;
using MediatR;
using Dapper;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Feature.Workspace.ShowMembers;

public class ShowMembersHandler : IRequestHandler<ShowMembersRequest, Result<Members, ErrorResponse>>
{
    private readonly IDbConnection _dbConnection;
    private readonly IHierarchyRepository _hierarchyRepository;
    public ShowMembersHandler(IDbConnection dbConnection, IHierarchyRepository hierarchyRepository)
    {
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
    }
    public async Task<Result<Members, ErrorResponse>> Handle(ShowMembersRequest request, CancellationToken cancellationToken)
    {
        var wokspace = await _hierarchyRepository.GetWorkspaceByIdAsync(request.workspaceId, cancellationToken);
        if (wokspace == null)
        {
            return Result<Members, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found."));
        }

        var sql = @"SELECT  u.""Id"",
                            u.""Name"",
                            u.""Email"",
                            uw.""Role""
                    FROM ""Users"" AS u
                    INNER JOIN ""UserWorkspaces"" AS uw ON u.""Id"" = uw.""UserId""
                    WHERE uw.""WorkspaceId"" = @WorkspaceId";

        var members = await _dbConnection.QueryAsync<Member>(sql, new { WorkspaceId = request.workspaceId });

        return Result<Members, ErrorResponse>.Success(new Members(members.ToList()));
    }
}
