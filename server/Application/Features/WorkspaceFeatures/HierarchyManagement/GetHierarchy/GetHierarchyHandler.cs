using Application.Contract.WorkspaceContract;
using Application.Helper;
using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public class GetHierarchyHandler : BaseQueryHandler, IRequestHandler<GetHierarchyQuery, WorkspaceHierarchyDto>
{
    public GetHierarchyHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,
        CursorHelper cursorHelper)
        : base(unitOfWork, currentUserService, workspaceContext, cursorHelper)
    {
    }

    public async Task<WorkspaceHierarchyDto> Handle(GetHierarchyQuery request, CancellationToken cancellationToken)
    {
        var workspace = await QueryNoTracking<ProjectWorkspace>()
            .Where(w => w.Id == request.WorkspaceId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Workspace {request.WorkspaceId} not found");



        // Use raw SQL to fetch hierarchy with permission filtering
        // This query gets all spaces, folders, and lists that the user can see
        // - Public items are always visible
        // - Private items are only visible if user has EntityMember relationship
        
        var sql = @"
            WITH user_spaces AS (
                SELECT 
                    s.id,
                    s.name,
                    s.color,
                    s.icon,
                    s.is_private,
                    s.order_key
                FROM project_spaces s
                WHERE s.project_workspace_id = @WorkspaceId
                  AND s.deleted_at IS NULL
                  AND s.is_archived = false
                  AND (
                      s.is_private = false 
                      OR EXISTS (
                          SELECT 1 FROM entity_accesses ea
                          INNER JOIN workspace_members wm ON ea.workspace_member_id = wm.id
                          WHERE ea.entity_id = s.id 
                            AND wm.user_id = @UserId 
                            AND ea.entity_layer = 1  -- Space
                            AND ea.deleted_at IS NULL
                      )
                  )
            ),
            user_folders AS (
                SELECT 
                    f.id,
                    f.project_space_id,
                    f.name,
                    f.color,
                    f.icon,
                    f.is_private,
                    f.order_key
                FROM project_folders f
                INNER JOIN user_spaces us ON f.project_space_id = us.id
                WHERE f.deleted_at IS NULL
                  AND f.is_archived = false
                  AND (
                      f.is_private = false 
                      OR EXISTS (
                          SELECT 1 FROM entity_accesses ea
                          INNER JOIN workspace_members wm ON ea.workspace_member_id = wm.id
                          WHERE ea.entity_id = f.id 
                            AND wm.user_id = @UserId 
                            AND ea.entity_layer = 2  -- Folder
                            AND ea.deleted_at IS NULL
                      )
                  )
            ),
            user_lists AS (
                SELECT 
                    l.id,
                    l.project_space_id,
                    l.project_folder_id,
                    l.name,
                    l.color,
                    l.icon,
                    l.is_private,
                    l.order_key
                FROM project_lists l
                INNER JOIN user_spaces us ON l.project_space_id = us.id
                WHERE l.deleted_at IS NULL
                  AND l.is_archived = false
                  AND (
                      l.is_private = false 
                      OR EXISTS (
                          SELECT 1 FROM entity_accesses ea
                          INNER JOIN workspace_members wm ON ea.workspace_member_id = wm.id
                          WHERE ea.entity_id = l.id 
                            AND wm.user_id = @UserId 
                            AND ea.entity_layer = 3  -- List
                            AND ea.deleted_at IS NULL
                      )
                  )
                  AND (
                      -- List must be either directly under space (no folder) or under a visible folder
                      l.project_folder_id IS NULL 
                      OR EXISTS (SELECT 1 FROM user_folders uf WHERE uf.id = l.project_folder_id)
                  )
            )
            SELECT 
                'Space' as item_type,
                id,
                project_space_id::text as parent_id,
                name,
                color,
                icon,
                is_private,
                order_key,
                NULL::uuid as project_folder_id
            FROM user_spaces
            UNION ALL
            SELECT 
                'Folder' as item_type,
                id,
                project_space_id::text as parent_id,
                name,
                color,
                icon,
                is_private,
                order_key,
                NULL::uuid as project_folder_id
            FROM user_folders
            UNION ALL
            SELECT 
                'List' as item_type,
                id,
                COALESCE(project_folder_id::text, project_space_id::text) as parent_id,
                name,
                color,
                icon,
                is_private,
                order_key,
                project_folder_id
            FROM user_lists
            ORDER BY order_key;
        ";

        var results = await UnitOfWork.QueryAsync<HierarchyRawItem>(sql, new 
        { 
            WorkspaceId = request.WorkspaceId, 
            UserId = CurrentUserId 
        }, cancellationToken);

        // Group results by type
        var items = results.ToList();
        var spaces = items.Where(i => i.ItemType == "Space").ToList();
        var folders = items.Where(i => i.ItemType == "Folder").ToList();
        var lists = items.Where(i => i.ItemType == "List").ToList();

        // Build hierarchy
        var spaceHierarchy = spaces.Select(s => new SpaceHierarchyDto
        {
            Id = s.Id,
            Name = s.Name,
            Color = s.Color,
            Icon = s.Icon,
            IsPrivate = s.IsPrivate,
            Folders = folders
                .Where(f => Guid.Parse(f.ParentId) == s.Id)
                .Select(f => new FolderHierarchyDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Color = f.Color,
                    Icon = f.Icon,
                    IsPrivate = f.IsPrivate,
                    Lists = lists
                        .Where(l => l.ProjectFolderId.HasValue && l.ProjectFolderId.Value == f.Id)
                        .Select(l => new ListHierarchyDto
                        {
                            Id = l.Id,
                            Name = l.Name,
                            Color = l.Color,
                            Icon = l.Icon,
                            IsPrivate = l.IsPrivate
                        })
                        .ToList()
                })
                .ToList(),
            Lists = lists
                .Where(l => !l.ProjectFolderId.HasValue && Guid.Parse(l.ParentId) == s.Id)
                .Select(l => new ListHierarchyDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    Color = l.Color,
                    Icon = l.Icon,
                    IsPrivate = l.IsPrivate
                })
                .ToList()
        }).ToList();

        return new WorkspaceHierarchyDto
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Spaces = spaceHierarchy
        };
    }

    // Internal DTO for raw SQL results
    private class HierarchyRawItem
    {
        public string ItemType { get; set; } = null!;
        public Guid Id { get; set; }
        public string ParentId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Color { get; set; } = null!;
        public string Icon { get; set; } = null!;
        public bool IsPrivate { get; set; }
        public long OrderKey { get; set; }
        public Guid? ProjectFolderId { get; set; }
    }
}
