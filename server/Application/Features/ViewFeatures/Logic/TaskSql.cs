using Application.Interfaces.Repositories;
using Domain.Enums.RelationShip;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Domain.Entities.ProjectEntities;
using Application.Interfaces;

namespace Application.Features.ViewFeatures.Logic;

public static class TaskSql
{
    public static async Task<object> Execute(IServiceProvider sp, Guid layerId, EntityLayerType layerType)
    {
        var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
        var statusResolver = sp.GetRequiredService<IStatusResolver>();
        var currentUserService = sp.GetRequiredService<server.Application.Interfaces.ICurrentUserService>();
        var userId = currentUserService.CurrentUserId();

        var sql = layerType switch
        {
            EntityLayerType.ProjectWorkspace => WorkspaceTasksSql,
            EntityLayerType.ProjectSpace => SpaceTasksSql,
            EntityLayerType.ProjectFolder => FolderTasksSql,
            EntityLayerType.ProjectList => ListTasksSql,
            _ => throw new NotSupportedException($"LayerType {layerType} is not supported for Task views.")
        };

        var tasks = await unitOfWork.QueryAsync<dynamic>(sql, new { layerId, UserId = userId });

        // Resolve active statuses for this layer (bubble up logic)
        var effectiveLayer = await statusResolver.ResolveEffectiveLayer(layerId, layerType);
        
        var statuses = await unitOfWork.Set<Status>()
            .Where(s => s.LayerId == effectiveLayer.LayerId && s.LayerType == effectiveLayer.LayerType && s.DeletedAt == null)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        return new 
        {
            Tasks = tasks,
            Statuses = statuses
        };
    }

    private const string WorkspaceTasksSql = @"
        SELECT DISTINCT t.* 
        FROM project_tasks t
        JOIN project_lists l ON t.project_list_id = l.id
        JOIN project_spaces s ON l.project_space_id = s.id
        LEFT JOIN project_folders f ON l.project_folder_id = f.id
        -- List access
        LEFT JOIN entity_access ea_l ON ea_l.entity_id = l.id 
            AND ea_l.entity_layer = 'ProjectList' 
            AND ea_l.deleted_at IS NULL
        LEFT JOIN workspace_members wm_l ON wm_l.id = ea_l.workspace_member_id 
            AND wm_l.user_id = @UserId 
            AND wm_l.deleted_at IS NULL
        -- Space access
        LEFT JOIN entity_access ea_s ON ea_s.entity_id = s.id 
            AND ea_s.entity_layer = 'ProjectSpace' 
            AND ea_s.deleted_at IS NULL
        LEFT JOIN workspace_members wm_s ON wm_s.id = ea_s.workspace_member_id 
            AND wm_s.user_id = @UserId 
            AND wm_s.deleted_at IS NULL
        -- Folder access
        LEFT JOIN entity_access ea_f ON ea_f.entity_id = f.id 
            AND ea_f.entity_layer = 'ProjectFolder' 
            AND ea_f.deleted_at IS NULL
        LEFT JOIN workspace_members wm_f ON wm_f.id = ea_f.workspace_member_id 
            AND wm_f.user_id = @UserId 
            AND wm_f.deleted_at IS NULL
        WHERE s.project_workspace_id = @layerId
        AND t.is_archived = false
        AND (s.is_private = false OR wm_s.id IS NOT NULL)
        AND (f.id IS NULL OR f.is_private = false OR wm_f.id IS NOT NULL)
        AND (l.is_private = false OR wm_l.id IS NOT NULL)
    ";

    private const string SpaceTasksSql = @"
        SELECT DISTINCT t.* 
        FROM project_tasks t
        JOIN project_lists l ON t.project_list_id = l.id
        LEFT JOIN project_folders f ON l.project_folder_id = f.id
        -- List access
        LEFT JOIN entity_access ea_l ON ea_l.entity_id = l.id 
            AND ea_l.entity_layer = 'ProjectList' 
            AND ea_l.deleted_at IS NULL
        LEFT JOIN workspace_members wm_l ON wm_l.id = ea_l.workspace_member_id 
            AND wm_l.user_id = @UserId 
            AND wm_l.deleted_at IS NULL
        -- Folder access
        LEFT JOIN entity_access ea_f ON ea_f.entity_id = f.id 
            AND ea_f.entity_layer = 'ProjectFolder' 
            AND ea_f.deleted_at IS NULL
        LEFT JOIN workspace_members wm_f ON wm_f.id = ea_f.workspace_member_id 
            AND wm_f.user_id = @UserId 
            AND wm_f.deleted_at IS NULL
        WHERE l.project_space_id = @layerId 
        AND t.is_archived = false
        AND (f.id IS NULL OR f.is_private = false OR wm_f.id IS NOT NULL)
        AND (l.is_private = false OR wm_l.id IS NOT NULL)
    ";

    private const string FolderTasksSql = @"
        SELECT DISTINCT t.* 
        FROM project_tasks t
        JOIN project_lists l ON t.project_list_id = l.id
        -- List access
        LEFT JOIN entity_access ea_l ON ea_l.entity_id = l.id 
            AND ea_l.entity_layer = 'ProjectList' 
            AND ea_l.deleted_at IS NULL
        LEFT JOIN workspace_members wm_l ON wm_l.id = ea_l.workspace_member_id 
            AND wm_l.user_id = @UserId 
            AND wm_l.deleted_at IS NULL
        WHERE l.project_folder_id = @layerId 
        AND t.is_archived = false
        AND (l.is_private = false OR wm_l.id IS NOT NULL)
    ";

    private const string ListTasksSql = @"
        SELECT * 
        FROM project_tasks 
        WHERE project_list_id = @layerId AND is_archived = false";
}
