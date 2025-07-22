using System;
using System.Data;
using Dapper;
using MediatR;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Feature.Workspace.DashboardWorkspace.GetLists;

public class GetListsHandler : IRequestHandler<GetListsRequest, Result<ListItems, ErrorResponse>>
{
    private readonly IDbConnection _dbConnection;
    private readonly IHierarchyRepository _hierarchyRepository;
    public GetListsHandler(IDbConnection dbConnection, IHierarchyRepository hierarchyRepository)
    {
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
    }
    public async Task<Result<ListItems, ErrorResponse>> Handle(GetListsRequest request, CancellationToken cancellationToken)
    {
        var workspace = await _hierarchyRepository.GetWorkspaceByIdAsync(request.workspaceId, cancellationToken);
        if (workspace == null)
        {
            return Result<ListItems, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found"));
        }
        var sql = @"SELECT ""Id"",
                           ""Name""
                    FRROM ""Lists""
                    WHERE ""WorkspaceId"" = @WorkspaceId";
        var lists = await _dbConnection.QueryAsync<ListItem>(sql , new { WorkspaceId = request.workspaceId });
        return Result<ListItems, ErrorResponse>.Success(new ListItems(lists.ToList()));
    }
}
