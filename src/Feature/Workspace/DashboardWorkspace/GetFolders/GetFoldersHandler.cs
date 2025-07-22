using System;
using System.Data;
using Dapper;
using MediatR;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Feature.Workspace.DashboardWorkspace.GetFolders;

public class GetFoldersHandler : IRequestHandler<GetFoldersRequest, Result<FolderItems, ErrorResponse>>
{
    private readonly IDbConnection _dbConnection;
    private readonly IHierarchyRepository _hierarchyRepository;
    public GetFoldersHandler(IDbConnection dbConnection, IHierarchyRepository hierarchyRepository)
    {
        _hierarchyRepository = hierarchyRepository;
        _dbConnection = dbConnection;
    }
    public async Task<Result<FolderItems, ErrorResponse>> Handle(GetFoldersRequest request, CancellationToken cancellationToken)
    {
        var workspace = await _hierarchyRepository.GetWorkspaceByIdAsync(request.workspaceId, cancellationToken);
        if (workspace == null)
        {
            return Result<FolderItems, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found"));
        }
        var sql = @"SELECT ""Id"" ,
                           ""Name""
                           FROM ""Folders""
                           WHERE ""WorkspaceId"" = @WorkspaceIda";
        var folders = await _dbConnection.QueryAsync<FolderItem>(sql, new { WorkspaceId = request.workspaceId });
        return Result<FolderItems, ErrorResponse>.Success(new FolderItems(folders.ToList()));
    }
}
