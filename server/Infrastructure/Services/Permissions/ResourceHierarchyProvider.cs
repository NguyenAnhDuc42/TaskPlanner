using Application.Interfaces.Services.Permissions;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Support.Workspace;
using Domain.Enums.RelationShip;
using Domain.Permission;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Permissions;

public sealed class ResourceHierarchyProvider : IResourceHierarchyProvider
{
    private readonly TaskPlanDbContext _context;

    public ResourceHierarchyProvider(TaskPlanDbContext context)
    {
        _context = context;
    }

    public async Task<EntityPath> GetPathToRoot(
        Guid entityId,
        EntityLayerType entityLayer,
        CancellationToken ct)
    {
        return entityLayer switch
        {
            EntityLayerType.ProjectTask => await GetTaskPath(entityId, ct),
            EntityLayerType.ProjectList => await GetListPath(entityId, ct),
            EntityLayerType.ProjectFolder => await GetFolderPath(entityId, ct),
            EntityLayerType.ProjectSpace => await GetSpacePath(entityId, ct),
            EntityLayerType.ChatRoom => await GetChatRoomPath(entityId, ct),
            EntityLayerType.ProjectWorkspace => await GetWorkspacePath(entityId, ct),
            _ => throw new InvalidOperationException($"Unknown entity layer: {entityLayer}")
        };
    }

    private async Task<EntityPath> GetTaskPath(Guid taskId, CancellationToken ct)
    {
        var task = await _context.ProjectTasks
            .AsNoTracking()
            .Select(t => new { t.Id, t.ProjectListId })
            .FirstOrDefaultAsync(x => x.Id == taskId, ct)
            ?? throw new KeyNotFoundException($"Task {taskId} not found");

        var listPath = await GetListPath(task.ProjectListId, ct);

        return new EntityPath
        {
            EntityId = taskId,
            EntityLayer = EntityLayerType.ProjectTask,
            IsPrivate = listPath.IsPrivate, // Task inherits privacy from list
            DirectParentId = task.ProjectListId,
            DirectParentType = EntityLayerType.ProjectList,
            ProjectWorkspaceId = listPath.ProjectWorkspaceId,
            ProjectSpaceId = listPath.ProjectSpaceId,
            ProjectFolderId = listPath.ProjectFolderId,
            ProjectListId = task.ProjectListId
        };
    }

    private async Task<EntityPath> GetListPath(Guid listId, CancellationToken ct)
    {
        var list = await _context.ProjectLists
            .AsNoTracking()
            .Select(l => new { l.Id, l.ProjectSpaceId, l.ProjectFolderId, l.IsPrivate })
            .FirstOrDefaultAsync(x => x.Id == listId, ct)
            ?? throw new KeyNotFoundException($"List {listId} not found");

        var space = await _context.ProjectSpaces
            .AsNoTracking()
            .Select(s => new { s.Id, s.ProjectWorkspaceId })
            .FirstOrDefaultAsync(x => x.Id == list.ProjectSpaceId, ct)
            ?? throw new KeyNotFoundException($"Space {list.ProjectSpaceId} not found");

        return new EntityPath
        {
            EntityId = listId,
            EntityLayer = EntityLayerType.ProjectList,
            IsPrivate = list.IsPrivate,
            DirectParentId = list.ProjectFolderId ?? list.ProjectSpaceId,
            DirectParentType = list.ProjectFolderId.HasValue ? EntityLayerType.ProjectFolder : EntityLayerType.ProjectSpace,
            ProjectWorkspaceId = space.ProjectWorkspaceId,
            ProjectSpaceId = list.ProjectSpaceId,
            ProjectFolderId = list.ProjectFolderId,
            ProjectListId = listId
        };
    }

    private async Task<EntityPath> GetFolderPath(Guid folderId, CancellationToken ct)
    {
        var folder = await _context.ProjectFolders
            .AsNoTracking()
            .Select(f => new { f.Id, f.ProjectSpaceId, f.IsPrivate })
            .FirstOrDefaultAsync(x => x.Id == folderId, ct)
            ?? throw new KeyNotFoundException($"Folder {folderId} not found");

        var space = await _context.ProjectSpaces
            .AsNoTracking()
            .Select(s => new { s.Id, s.ProjectWorkspaceId })
            .FirstOrDefaultAsync(x => x.Id == folder.ProjectSpaceId, ct)
            ?? throw new KeyNotFoundException($"Space {folder.ProjectSpaceId} not found");

        return new EntityPath
        {
            EntityId = folderId,
            EntityLayer = EntityLayerType.ProjectFolder,
            IsPrivate = folder.IsPrivate,
            DirectParentId = folder.ProjectSpaceId,
            DirectParentType = EntityLayerType.ProjectSpace,
            ProjectWorkspaceId = space.ProjectWorkspaceId,
            ProjectSpaceId = folder.ProjectSpaceId,
            ProjectFolderId = folderId,
            ProjectListId = null
        };
    }

    private async Task<EntityPath> GetSpacePath(Guid spaceId, CancellationToken ct)
    {
        var space = await _context.ProjectSpaces
            .AsNoTracking()
            .Select(s => new { s.Id, s.ProjectWorkspaceId, s.IsPrivate })
            .FirstOrDefaultAsync(x => x.Id == spaceId, ct)
            ?? throw new KeyNotFoundException($"Space {spaceId} not found");

        return new EntityPath
        {
            EntityId = spaceId,
            EntityLayer = EntityLayerType.ProjectSpace,
            IsPrivate = space.IsPrivate,
            DirectParentId = space.ProjectWorkspaceId,
            DirectParentType = EntityLayerType.ProjectWorkspace,
            ProjectWorkspaceId = space.ProjectWorkspaceId,
            ProjectSpaceId = spaceId,
            ProjectFolderId = null,
            ProjectListId = null
        };
    }

    private async Task<EntityPath> GetChatRoomPath(Guid chatRoomId, CancellationToken ct)
    {
        var chatRoom = await _context.ChatRooms
            .AsNoTracking()
            .Select(c => new { c.Id, c.ProjectWorkspaceId, c.IsPrivate })
            .FirstOrDefaultAsync(x => x.Id == chatRoomId, ct)
            ?? throw new KeyNotFoundException($"ChatRoom {chatRoomId} not found");

        return new EntityPath
        {
            EntityId = chatRoomId,
            EntityLayer = EntityLayerType.ChatRoom,
            IsPrivate = chatRoom.IsPrivate,
            DirectParentId = chatRoom.ProjectWorkspaceId,
            DirectParentType = EntityLayerType.ProjectWorkspace,
            ProjectWorkspaceId = chatRoom.ProjectWorkspaceId,
            ProjectSpaceId = null,
            ProjectFolderId = null,
            ProjectListId = null
        };
    }

    private async Task<EntityPath> GetWorkspacePath(Guid workspaceId, CancellationToken ct)
    {
        var workspace = await _context.ProjectWorkspaces
            .AsNoTracking()
            .Select(w => new { w.Id })
            .FirstOrDefaultAsync(x => x.Id == workspaceId, ct)
            ?? throw new KeyNotFoundException($"Workspace {workspaceId} not found");

        return new EntityPath
        {
            EntityId = workspaceId,
            EntityLayer = EntityLayerType.ProjectWorkspace,
            IsPrivate = false,
            DirectParentId = null,
            DirectParentType = null,
            ProjectWorkspaceId = workspaceId,
            ProjectSpaceId = null,
            ProjectFolderId = null,
            ProjectListId = null
        };
    }
}
