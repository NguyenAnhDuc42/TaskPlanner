using Application.Common;
using Application.Interfaces.Services.Permissions;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Infrastructure.Services.Permissions;

public class HierarchyService : IHierarchyService
{
    private readonly TaskPlanDbContext _dbContext;

    public HierarchyService(TaskPlanDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HierarchyPath> ResolvePathAsync(Guid entityId, EntityType type, CancellationToken ct = default)
    {
        return type switch
        {
            EntityType.ProjectTask => await ResolveTaskPathAsync(entityId, ct),
            EntityType.ProjectList => await ResolveListPathAsync(entityId, ct),
            EntityType.ProjectFolder => await ResolveFolderPathAsync(entityId, ct),
            EntityType.ProjectSpace => await ResolveSpacePathAsync(entityId, ct),
            EntityType.ProjectWorkspace => await ResolveWorkspacePathAsync(entityId, ct),
            EntityType.ChatRoom => await ResolveChatRoomPathAsync(entityId, ct),
            EntityType.ChatMessage => await ResolveChatMessagePathAsync(entityId, ct),
            _ => new HierarchyPath { IsValid = false }
        };
    }

    private async Task<HierarchyPath> ResolveTaskPathAsync(Guid id, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                t.id as TargetId, t.created_by as TargetCreatorId, t.is_archived as TargetIsArchived,
                l.id as ListId, l.is_private as ListIsPrivate,
                f.id as FolderId, f.is_private as FolderIsPrivate,
                s.id as SpaceId, s.is_private as SpaceIsPrivate,
                w.id as WorkspaceId
            FROM project_tasks t
            JOIN project_lists l ON t.project_list_id = l.id
            LEFT JOIN project_folders f ON l.project_folder_id = f.id
            JOIN project_spaces s ON l.project_space_id = s.id
            JOIN project_workspaces w ON s.project_workspace_id = w.id
            WHERE t.id = @id";

        var conn = _dbContext.Database.GetDbConnection();
        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { id });
        
        if (result == null) return new HierarchyPath { IsValid = false };

        var path = new HierarchyPath
        {
            TargetId = id,
            TargetType = EntityType.ProjectTask,
            WorkspaceId = result.workspaceid,
            TargetIsPrivate = false, // Tasks don't have IsPrivate; they inherit from List
            TargetCreatorId = result.targetcreatorid,
            TargetIsArchived = result.targetisarchived,
            IsValid = true
        };

        path.Ancestors.Add(new EntityPathNode { Id = result.listid, Type = EntityType.ProjectList, IsPrivate = result.listisprivate });
        if (result.folderid != null)
            path.Ancestors.Add(new EntityPathNode { Id = result.folderid, Type = EntityType.ProjectFolder, IsPrivate = result.folderisprivate });
        path.Ancestors.Add(new EntityPathNode { Id = result.spaceid, Type = EntityType.ProjectSpace, IsPrivate = result.spaceisprivate });

        return path;
    }

    private async Task<HierarchyPath> ResolveListPathAsync(Guid id, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                l.id as TargetId, l.is_private as TargetIsPrivate, l.creator_id as TargetCreatorId, l.is_archived as TargetIsArchived,
                f.id as FolderId, f.is_private as FolderIsPrivate,
                s.id as SpaceId, s.is_private as SpaceIsPrivate,
                w.id as WorkspaceId
            FROM project_lists l
            LEFT JOIN project_folders f ON l.project_folder_id = f.id
            JOIN project_spaces s ON l.project_space_id = s.id
            JOIN project_workspaces w ON s.project_workspace_id = w.id
            WHERE l.id = @id";

        var conn = _dbContext.Database.GetDbConnection();
        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { id });
        
        if (result == null) return new HierarchyPath { IsValid = false };

        var path = new HierarchyPath
        {
            TargetId = id,
            TargetType = EntityType.ProjectList,
            WorkspaceId = result.workspaceid,
            TargetIsPrivate = result.targetisprivate,
            TargetCreatorId = result.targetcreatorid,
            TargetIsArchived = result.targetisarchived,
            IsValid = true
        };

        if (result.folderid != null)
            path.Ancestors.Add(new EntityPathNode { Id = result.folderid, Type = EntityType.ProjectFolder, IsPrivate = result.folderisprivate });
        path.Ancestors.Add(new EntityPathNode { Id = result.spaceid, Type = EntityType.ProjectSpace, IsPrivate = result.spaceisprivate });

        return path;
    }

    private async Task<HierarchyPath> ResolveFolderPathAsync(Guid id, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                f.id as TargetId, f.is_private as TargetIsPrivate, f.creator_id as TargetCreatorId, f.is_archived as TargetIsArchived,
                s.id as SpaceId, s.is_private as SpaceIsPrivate,
                w.id as WorkspaceId
            FROM project_folders f
            JOIN project_spaces s ON f.project_space_id = s.id
            JOIN project_workspaces w ON s.project_workspace_id = w.id
            WHERE f.id = @id";

        var conn = _dbContext.Database.GetDbConnection();
        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { id });
        
        if (result == null) return new HierarchyPath { IsValid = false };

        var path = new HierarchyPath
        {
            TargetId = id,
            TargetType = EntityType.ProjectFolder,
            WorkspaceId = result.workspaceid,
            TargetIsPrivate = result.targetisprivate,
            TargetCreatorId = result.targetcreatorid,
            TargetIsArchived = result.targetisarchived,
            IsValid = true
        };

        path.Ancestors.Add(new EntityPathNode { Id = result.spaceid, Type = EntityType.ProjectSpace, IsPrivate = result.spaceisprivate });

        return path;
    }

    private async Task<HierarchyPath> ResolveSpacePathAsync(Guid id, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                s.id as TargetId, s.is_private as TargetIsPrivate, s.creator_id as TargetCreatorId, s.is_archived as TargetIsArchived,
                w.id as WorkspaceId
            FROM project_spaces s
            JOIN project_workspaces w ON s.project_workspace_id = w.id
            WHERE s.id = @id";

        var conn = _dbContext.Database.GetDbConnection();
        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { id });
        
        if (result == null) return new HierarchyPath { IsValid = false };

        return new HierarchyPath
        {
            TargetId = id,
            TargetType = EntityType.ProjectSpace,
            WorkspaceId = result.workspaceid,
            TargetIsPrivate = result.targetisprivate,
            TargetCreatorId = result.targetcreatorid,
            TargetIsArchived = result.targetisarchived,
            IsValid = true
        };
    }

    private async Task<HierarchyPath> ResolveWorkspacePathAsync(Guid id, CancellationToken ct)
    {
        const string sql = "SELECT id, creator_id as TargetCreatorId, is_archived as TargetIsArchived FROM project_workspaces WHERE id = @id";
        var conn = _dbContext.Database.GetDbConnection();
        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { id });

        if (result == null) return new HierarchyPath { IsValid = false };

        return new HierarchyPath
        {
            TargetId = id,
            TargetType = EntityType.ProjectWorkspace,
            WorkspaceId = id,
            TargetIsPrivate = false, // Workspaces are public boundaries
            TargetCreatorId = result.targetcreatorid,
            TargetIsArchived = result.targetisarchived,
            IsValid = true
        };
    }

    private async Task<HierarchyPath> ResolveChatRoomPathAsync(Guid id, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                cr.id as TargetId, cr.is_private as TargetIsPrivate, cr.creator_id as TargetCreatorId,
                w.id as WorkspaceId
            FROM chat_rooms cr
            JOIN project_workspaces w ON cr.project_workspace_id = w.id
            WHERE cr.id = @id";

        var conn = _dbContext.Database.GetDbConnection();
        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { id });
        
        if (result == null) return new HierarchyPath { IsValid = false };

        return new HierarchyPath
        {
            TargetId = id,
            TargetType = EntityType.ChatRoom,
            WorkspaceId = result.workspaceid,
            TargetIsPrivate = result.targetisprivate,
            TargetCreatorId = result.targetcreatorid,
            TargetIsArchived = false, // Rooms don't have is_archived yet
            IsValid = true
        };
    }

    private async Task<HierarchyPath> ResolveChatMessagePathAsync(Guid id, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                m.id as TargetId, m.sender_id as TargetCreatorId,
                cr.id as ChatRoomId, cr.is_private as ChatRoomIsPrivate,
                w.id as WorkspaceId
            FROM chat_messages m
            JOIN chat_rooms cr ON m.chat_room_id = cr.id
            JOIN project_workspaces w ON cr.project_workspace_id = w.id
            WHERE m.id = @id";

        var conn = _dbContext.Database.GetDbConnection();
        var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { id });
        
        if (result == null) return new HierarchyPath { IsValid = false };

        var path = new HierarchyPath
        {
            TargetId = id,
            TargetType = EntityType.ChatMessage,
            WorkspaceId = result.workspaceid,
            TargetIsPrivate = false, // Messages inherit room privacy
            TargetCreatorId = result.targetcreatorid,
            TargetIsArchived = false,
            IsValid = true
        };

        path.Ancestors.Add(new EntityPathNode { Id = result.chatroomid, Type = EntityType.ChatRoom, IsPrivate = result.chatroomisprivate });

        return path;
    }
}
