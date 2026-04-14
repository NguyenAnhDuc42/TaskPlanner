using Application.Interfaces.Repositories;
using Domain.Enums.RelationShip;

namespace Application.Helpers;

public class AncestorChain
{
    public Guid ProjectWorkspaceId { get; set; }
    public Guid? ProjectSpaceId { get; set; }
    public Guid? ProjectFolderId { get; set; }

    public AncestorChain() { }

    public AncestorChain(Guid projectWorkspaceId, Guid? projectSpaceId, Guid? projectFolderId)
    {
        ProjectWorkspaceId = projectWorkspaceId;
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
    }
}

public static class HierarchyHelper
{
    public static async Task<AncestorChain> GetAncestorChain(
        IUnitOfWork unitOfWork,
        Guid parentId,
        EntityLayerType parentType,
        CancellationToken ct = default)
    {
        return parentType switch
        {
            EntityLayerType.ProjectFolder => await GetFromFolder(unitOfWork, parentId, ct),
            EntityLayerType.ProjectSpace => await GetFromSpace(unitOfWork, parentId, ct),
            EntityLayerType.ProjectWorkspace => new AncestorChain(parentId, null, null),
            _ => throw new ArgumentException("Invalid layer type for hierarchy resolution.")
        };
    }

    private static async Task<AncestorChain> GetFromFolder(IUnitOfWork unitOfWork, Guid id, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                s.project_workspace_id AS ProjectWorkspaceId,
                f.project_space_id     AS ProjectSpaceId,
                f.id                   AS ProjectFolderId
            FROM  project_folders f
            JOIN  project_spaces s ON f.project_space_id = s.id
            WHERE f.id = @Id";

        var result = await unitOfWork.QuerySingleOrDefaultAsync<AncestorChain>(sql, new { Id = id }, ct);
        return result ?? throw new KeyNotFoundException($"ProjectFolder {id} not found.");
    }

    private static async Task<AncestorChain> GetFromSpace(IUnitOfWork unitOfWork, Guid id, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                project_workspace_id AS ProjectWorkspaceId,
                id                   AS ProjectSpaceId,
                NULL::uuid           AS ProjectFolderId
            FROM  project_spaces
            WHERE id = @Id";

        var result = await unitOfWork.QuerySingleOrDefaultAsync<AncestorChain>(sql, new { Id = id }, ct);
        return result ?? throw new KeyNotFoundException($"ProjectSpace {id} not found.");
    }
}
