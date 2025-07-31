using System;
using System.Data;
using Dapper;
using MediatR;
using src.Contract;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Feature.WorkspaceManager.DashboardWorkspace.GetLists;

public class GetListsHandler : IRequestHandler<GetListsRequest, Result<List<ListSummary>, ErrorResponse>>
{
    private readonly IDbConnection _dbConnection;
    private readonly IHierarchyRepository _hierarchyRepository;
    public GetListsHandler(IDbConnection dbConnection, IHierarchyRepository hierarchyRepository)
    {
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
    }
    public async Task<Result<List<ListSummary>, ErrorResponse>> Handle(GetListsRequest request, CancellationToken cancellationToken)
    {
        var workspace = await _hierarchyRepository.GetWorkspaceByIdAsync(request.workspaceId, cancellationToken);
        if (workspace == null)
        {
            return Result<List<ListSummary>, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found"));
        }
        var sql = @"SELECT ""Id"",
                           ""Name""
                    FRROM ""Lists""
                    WHERE ""WorkspaceId"" = @WorkspaceId";
        var lists = await _dbConnection.QueryAsync<ListSummary>(sql , new { WorkspaceId = request.workspaceId });
        return Result<List<ListSummary>, ErrorResponse>.Success(lists.ToList());
    }
}
