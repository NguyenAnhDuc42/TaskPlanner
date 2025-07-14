using System;
using System.Data;
using Dapper;
using MediatR;
using src.Helper.Results;

namespace src.Feature.Workspace.GetHierarchy;

public class GetHierarchyHandler : IRequestHandler<GetHierarchyRequest, Result<Hierarchy, ErrorResponse>>
{
    private readonly IDbConnection _dbConnection;

    public GetHierarchyHandler(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
    }
    public async Task<Result<Hierarchy, ErrorResponse>> Handle(GetHierarchyRequest request, CancellationToken cancellationToken)
    {
        var spaces = await GetSpacesAsync(request.workspaceId);
        var spaceIds = spaces.Select(s => s.Id).ToList();

        var folders = await GetFoldersAsync(spaceIds);
        var folderIds = folders.Select(f => f.Id).ToList();

        var lists = await GetListsAsync(spaceIds, folderIds);

        return Result<Hierarchy, ErrorResponse>.Success(BuildHierarchy(spaces, folders, lists));

    }
    private async Task<List<SpaceDto>> GetSpacesAsync(Guid workspaceId)
    {
        var sql = @"
            SELECT ""Id"", ""Name""
            FROM ""Spaces"" 
            WHERE ""WorkspaceId"" = @WorkspaceId
            ORDER BY ""CreatedAt""";

        return (await _dbConnection.QueryAsync<SpaceDto>(sql, new { WorkspaceId = workspaceId })).ToList();
    }

    private async Task<List<FolderDto>> GetFoldersAsync(List<Guid> spaceIds)
    {
        if (!spaceIds.Any()) return new List<FolderDto>();

        var sql = @"
            SELECT ""Id"", ""Name"", ""SpaceId""
            FROM ""Folders"" 
            WHERE ""SpaceId"" = ANY(@SpaceIds)
            ORDER BY ""Name""";

        return (await _dbConnection.QueryAsync<FolderDto>(sql, new { SpaceIds = spaceIds.ToArray() })).ToList();
    }

    private async Task<List<ListDto>> GetListsAsync(List<Guid> spaceIds, List<Guid> folderIds)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (spaceIds.Any())
        {
            conditions.Add(@"""SpaceId"" = ANY(@SpaceIds)");
            parameters.Add("SpaceIds", spaceIds.ToArray());
        }

        if (folderIds.Any())
        {
            conditions.Add(@"""FolderId"" = ANY(@FolderIds)");
            parameters.Add("FolderIds", folderIds.ToArray());
        }

        if (!conditions.Any()) return new List<ListDto>();

        var sql = $@"
            SELECT ""Id"", ""Name"", ""SpaceId"", ""FolderId""
            FROM Lists 
            WHERE {string.Join(" OR ", conditions)}
            ORDER BY ""Name""";

        return (await _dbConnection.QueryAsync<ListDto>(sql, parameters)).ToList();
    }
    private Hierarchy BuildHierarchy(
        List<SpaceDto> spaces, 
        List<FolderDto> folders, 
        List<ListDto> lists)
    {
        // Group folders by space
        var foldersBySpace = folders.GroupBy(f => f.SpaceId)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        // Group lists by parent (either space or folder)
        var listsBySpace = lists.Where(l => l.SpaceId.HasValue && !l.FolderId.HasValue)
            .GroupBy(l => l.SpaceId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
            
        var listsByFolder = lists.Where(l => l.FolderId.HasValue)
            .GroupBy(l => l.FolderId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        
        var spaceNodes = spaces.Select(space => new SpaceNode(
            space.Id,
            space.Name,
            // Build folders for this space
            foldersBySpace.GetValueOrDefault(space.Id, new List<FolderDto>())
                .Select(folder => new FolderNode(
                    folder.Id,
                    folder.Name,
                    // Build lists for this folder
                    listsByFolder.GetValueOrDefault(folder.Id, new List<ListDto>())
                        .Select(list => new ListNode(list.Id, list.Name))
                        .ToList()
                ))
                .ToList(),
            // Build direct lists for this space
            listsBySpace.GetValueOrDefault(space.Id, new List<ListDto>())
                .Select(list => new ListNode(list.Id, list.Name))
                .ToList()
        )).ToList();
        
        return new Hierarchy(spaceNodes);
    }
}