using Application.Contract.WorkspaceContract;
using Application.Helper;
using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public class GetHierarchyHandler : BaseFeatureHandler, IRequestHandler<GetHierarchyQuery, WorkspaceHierarchyDto>
{
    private readonly CursorHelper _cursorHelper;
    private readonly ILogger<GetHierarchyHandler> _logger;

    public GetHierarchyHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,
        CursorHelper cursorHelper,
        ILogger<GetHierarchyHandler> logger)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
        _cursorHelper = cursorHelper ?? throw new ArgumentNullException(nameof(cursorHelper));
        _logger = logger;
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
                    s.project_workspace_id,
                    s.name,
                    s.custom_color,
                    s.custom_icon,
                    s.is_private,
                    s.order_key
                FROM project_spaces s
                WHERE s.project_workspace_id = @WorkspaceId
                  AND s.deleted_at IS NULL
                  AND s.is_archived = false
                  AND (
                      s.is_private = false 
                      OR EXISTS (
                          SELECT 1 FROM entity_access ea
                          INNER JOIN workspace_members wm ON ea.workspace_member_id = wm.id
                          WHERE ea.entity_id = s.id 
                            AND wm.user_id = @UserId 
                            AND wm.project_workspace_id = @WorkspaceId
                            AND ea.entity_layer = 'ProjectSpace'
                            AND ea.deleted_at IS NULL
                            AND wm.deleted_at IS NULL
                      )
                  )
            ),
            user_folders AS (
                SELECT 
                    f.id,
                    f.project_space_id,
                    f.name,
                    f.custom_color,
                    f.custom_icon,
                    f.is_private,
                    f.order_key
                FROM project_folders f
                INNER JOIN user_spaces us ON f.project_space_id = us.id
                WHERE f.deleted_at IS NULL
                  AND f.is_archived = false
                  AND (
                      f.is_private = false 
                      OR EXISTS (
                          SELECT 1 FROM entity_access ea
                          INNER JOIN workspace_members wm ON ea.workspace_member_id = wm.id
                          WHERE ea.entity_id = f.id 
                            AND wm.user_id = @UserId 
                            AND wm.project_workspace_id = @WorkspaceId
                            AND ea.entity_layer = 'ProjectFolder'
                            AND ea.deleted_at IS NULL
                            AND wm.deleted_at IS NULL
                      )
                  )
            ),
            user_lists AS (
                SELECT 
                    l.id,
                    l.project_space_id,
                    l.project_folder_id,
                    l.name,
                    l.custom_color,
                    l.custom_icon,
                    l.is_private,
                    l.order_key
                FROM project_lists l
                INNER JOIN user_spaces us ON l.project_space_id = us.id
                WHERE l.deleted_at IS NULL
                  AND l.is_archived = false
                  AND (
                      l.is_private = false 
                      OR EXISTS (
                          SELECT 1 FROM entity_access ea
                          INNER JOIN workspace_members wm ON ea.workspace_member_id = wm.id
                          WHERE ea.entity_id = l.id 
                            AND wm.user_id = @UserId 
                            AND wm.project_workspace_id = @WorkspaceId
                            AND ea.entity_layer = 'ProjectList'
                            AND ea.deleted_at IS NULL
                            AND wm.deleted_at IS NULL
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
                project_workspace_id::text as parent_id,
                name,
                custom_color,
                custom_icon,
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
                custom_color,
                custom_icon,
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
                custom_color,
                custom_icon,
                is_private,
                order_key,
                project_folder_id
            FROM user_lists
            ORDER BY order_key;
        ";

        _logger.LogWarning("[HIERARCHY DEBUG] UserId={UserId}, WorkspaceId={WorkspaceId}", CurrentUserId, request.WorkspaceId);

        // DEBUG: Check what entity_access records exist for this user
        var debugAccessSql = @"
            SELECT ea.entity_id, ea.entity_layer, ea.access_level, wm.user_id
            FROM entity_access ea
            INNER JOIN workspace_members wm ON ea.workspace_member_id = wm.id
            WHERE wm.user_id = @UserId 
              AND wm.project_workspace_id = @WorkspaceId
              AND ea.deleted_at IS NULL
              AND wm.deleted_at IS NULL";
        var debugAccess = await UnitOfWork.QueryAsync<dynamic>(debugAccessSql, new { UserId = CurrentUserId, WorkspaceId = request.WorkspaceId }, cancellationToken);
        _logger.LogWarning("[HIERARCHY DEBUG] EntityAccess records for this user: {Count}", debugAccess.Count());
        foreach (var a in debugAccess)
        {
            _logger.LogWarning("[HIERARCHY DEBUG]   -> entity_id={EntityId}, layer={Layer}, access={Access}", (object)a.entity_id, (object)a.entity_layer, (object)a.access_level);
        }

        // DEBUG: Check how many private spaces exist total
        var debugPrivateSql = @"
            SELECT id, name, is_private 
            FROM project_spaces 
            WHERE project_workspace_id = @WorkspaceId AND deleted_at IS NULL AND is_private = true";
        var debugPrivate = await UnitOfWork.QueryAsync<dynamic>(debugPrivateSql, new { WorkspaceId = request.WorkspaceId }, cancellationToken);
        _logger.LogWarning("[HIERARCHY DEBUG] Total private spaces in DB: {Count}", debugPrivate.Count());
        foreach (var p in debugPrivate)
        {
            _logger.LogWarning("[HIERARCHY DEBUG]   -> id={Id}, name={Name}, is_private={IsPrivate}", (object)p.id, (object)p.name, (object)p.is_private);
        }

        // DEBUG: Check private folders
        var debugFolderSql = @"
            SELECT id, name, is_private, project_space_id 
            FROM project_folders 
            WHERE deleted_at IS NULL AND is_private = true";
        var debugFolders = await UnitOfWork.QueryAsync<dynamic>(debugFolderSql, cancellationToken: cancellationToken);
        _logger.LogWarning("[HIERARCHY DEBUG] Total private folders in DB: {Count}", debugFolders.Count());
        foreach (var f in debugFolders)
        {
            _logger.LogWarning("[HIERARCHY DEBUG]   -> id={Id}, name={Name}, is_private={IsPrivate}, space={SpaceId}", (object)f.id, (object)f.name, (object)f.is_private, (object)f.project_space_id);
        }

        var results = await UnitOfWork.QueryAsync<HierarchyRawItem>(sql, new 
        { 
            WorkspaceId = request.WorkspaceId, 
            UserId = CurrentUserId 
        }, cancellationToken);

        // Temporary debug: log ALL returned items with their privacy flag
        _logger.LogWarning("[HIERARCHY DEBUG] Total items returned: {Count}", results.Count());
        foreach (var item in results)
        {
            _logger.LogWarning("[HIERARCHY DEBUG]   -> {Type}: {Name} (id={Id}, is_private={IsPrivate})", 
                item.ItemType, item.Name, item.Id, item.IsPrivate);
        }

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
            Color = s.CustomColor,
            Icon = s.CustomIcon,
            IsPrivate = s.IsPrivate,
            Folders = folders
                .Where(f => Guid.Parse(f.ParentId) == s.Id)
                .Select(f => new FolderHierarchyDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Color = f.CustomColor,
                    Icon = f.CustomIcon,
                    IsPrivate = f.IsPrivate,
                    Lists = lists
                        .Where(l => l.ProjectFolderId.HasValue && l.ProjectFolderId.Value == f.Id)
                        .Select(l => new ListHierarchyDto
                        {
                            Id = l.Id,
                            Name = l.Name,
                            Color = l.CustomColor,
                            Icon = l.CustomIcon,
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
                    Color = l.CustomColor,
                    Icon = l.CustomIcon,
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
        public string CustomColor { get; set; } = null!;
        public string CustomIcon { get; set; } = null!;
        public bool IsPrivate { get; set; }
        public long OrderKey { get; set; }
        public Guid? ProjectFolderId { get; set; }
    }
}
