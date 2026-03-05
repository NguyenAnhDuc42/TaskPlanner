using Application.Contract.Common;
using Application.Contract.StatusContract;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;
using Application.Features.ViewFeatures.GetViewData;
using Domain.Entities.Relationship;

namespace Application.Features.ViewFeatures.Logic;

public class ViewBuilder
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStatusResolver _statusResolver;

    public ViewBuilder(
        IUnitOfWork unitOfWork,
        IStatusResolver statusResolver)
    {
        _unitOfWork = unitOfWork;
        _statusResolver = statusResolver;
    }

    public async Task<BaseViewResult> Build(
        Guid layerId,
        EntityLayerType layerType,
        ViewDefinition view,
        Guid workspaceMemberId)
    {
        return view.ViewType switch
        {
            ViewType.List  => await BuildListView(layerId, layerType, workspaceMemberId),
            ViewType.Board => await BuildBoardView(layerId, layerType, workspaceMemberId),
            ViewType.Doc   => await BuildDocView(layerId, layerType),
            _              => throw new NotSupportedException($"ViewType {view.ViewType} is not supported yet.")
        };
    }

    // ──────────────────────────────────────────────────────────
    // View-specific builders
    // ──────────────────────────────────────────────────────────

    private async Task<TaskListViewResult> BuildListView(Guid layerId, EntityLayerType layerType, Guid workspaceMemberId)
    {
        var (tasks, statuses) = await GetTasksAndStatuses(layerId, layerType, workspaceMemberId);
        return new TaskListViewResult(tasks, statuses);
    }

    private async Task<TasksBoardViewResult> BuildBoardView(Guid layerId, EntityLayerType layerType, Guid workspaceMemberId)
    {
        var (tasks, statuses) = await GetTasksAndStatuses(layerId, layerType, workspaceMemberId);
        return new TasksBoardViewResult(tasks, statuses);
    }

    private async Task<DocumentListResult> BuildDocView(Guid layerId, EntityLayerType layerType)
    {
        var statuses = await GetEffectiveStatuses(layerId, layerType);

        const string sql = @"
            SELECT id, layer_id, name, content
            FROM   documents
            WHERE  layer_id   = @LayerId
              AND  layer_type  = @LayerType
              AND  deleted_at IS NULL";

        var docs = await _unitOfWork.QueryAsync<DocumentDto>(sql, new
        {
            LayerId   = layerId,
            LayerType = layerType.ToString()
        });

        return new DocumentListResult(docs, statuses);
    }

    // ──────────────────────────────────────────────────────────
    // Shared helpers
    // ──────────────────────────────────────────────────────────

    private async Task<(IEnumerable<TaskDto> Tasks, IEnumerable<StatusDto> Statuses)> GetTasksAndStatuses(
        Guid layerId, EntityLayerType layerType, Guid workspaceMemberId)
    {
        // Delegate to TaskSql which already has the correct SQL per layer type,
        // including access-control joins (entity_access / workspace_members / private flags).
        var sql = TaskSql.GetSql(layerType);
        var tasks = (await _unitOfWork.QueryAsync<TaskDto>(sql, new { layerId, WorkspaceMemberId = workspaceMemberId })).ToList();

        await FetchAssignees(tasks);

        IEnumerable<StatusDto> statuses;
        if (layerType == EntityLayerType.ProjectList)
        {
            statuses = await GetEffectiveStatuses(layerId, layerType);
        }
        else
        {
            var scopedStatuses = (await GetStatusesForLayerScope(tasks, layerId, layerType, workspaceMemberId)).ToList();
            var canonical = CanonicalizeStatuses(scopedStatuses);

            foreach (var task in tasks)
            {
                if (task.StatusId.HasValue && canonical.StatusIdMap.TryGetValue(task.StatusId.Value, out var canonicalId))
                {
                    task.StatusId = canonicalId;
                }
            }

            statuses = canonical.Statuses;
        }

        return (tasks, statuses);
    }

    private static (List<StatusDto> Statuses, Dictionary<Guid, Guid> StatusIdMap) CanonicalizeStatuses(
        List<StatusDto> statuses)
    {
        var grouped = statuses
            .GroupBy(s => $"{s.Category}:{s.Name.Trim().ToLowerInvariant()}")
            .ToList();

        var canonical = new List<StatusDto>(grouped.Count);
        var statusIdMap = new Dictionary<Guid, Guid>(statuses.Count);

        foreach (var group in grouped)
        {
            var representative = group
                .OrderByDescending(s => s.IsDefault)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.Id)
                .First();

            canonical.Add(representative);

            foreach (var status in group)
            {
                statusIdMap[status.Id] = representative.Id;
            }
        }

        return (canonical, statusIdMap);
    }

    private async Task<IEnumerable<StatusDto>> GetStatusesForLayerScope(
        IReadOnlyCollection<TaskDto> tasks,
        Guid layerId,
        EntityLayerType layerType,
        Guid workspaceMemberId)
    {
        // Strict mode: include statuses from all accessible descendant lists in this layer scope,
        // even when no task currently uses them (so empty columns still render).
        var statuses = (await GetStatusesFromAccessibleDescendantLists(layerId, layerType, workspaceMemberId)).ToList();

        // Also merge any task-linked status IDs that might come from legacy data/config drift,
        // so tasks are never hidden due to missing status metadata.
        var visibleTaskStatusIds = tasks
            .Where(t => t.StatusId.HasValue)
            .Select(t => t.StatusId!.Value)
            .Distinct()
            .ToList();

        if (!visibleTaskStatusIds.Any())
        {
            return statuses;
        }

        var existingIds = statuses.Select(s => s.Id).ToHashSet();
        var missingTaskStatusIds = visibleTaskStatusIds.Where(id => !existingIds.Contains(id)).ToArray();
        if (missingTaskStatusIds.Length == 0)
        {
            return statuses;
        }

        const string sql = @"
            SELECT id, name, color, category, is_default_status AS IsDefault
            FROM   statuses
            WHERE  id = ANY(@StatusIds)
              AND  deleted_at IS NULL
            ORDER BY created_at";
        var extraStatuses = await _unitOfWork.QueryAsync<StatusDto>(sql, new { StatusIds = missingTaskStatusIds });

        foreach (var status in extraStatuses)
        {
            if (existingIds.Add(status.Id))
            {
                statuses.Add(status);
            }
        }

        return statuses;
    }

    private async Task<IEnumerable<StatusDto>> GetStatusesFromAccessibleDescendantLists(
        Guid layerId,
        EntityLayerType layerType,
        Guid workspaceMemberId)
    {
        var entityAccess = _unitOfWork.Set<EntityAccess>().AsNoTracking();

        var scopedQuery =
            from l in _unitOfWork.Set<ProjectList>().AsNoTracking()
            join s in _unitOfWork.Set<ProjectSpace>().AsNoTracking() on l.ProjectSpaceId equals s.Id
            join f0 in _unitOfWork.Set<ProjectFolder>().AsNoTracking() on l.ProjectFolderId equals f0.Id into folderJoin
            from f in folderJoin.DefaultIfEmpty()
            where l.DeletedAt == null
               && s.DeletedAt == null
               && (f == null || f.DeletedAt == null)
            select new { l, s, f };

        scopedQuery = layerType switch
        {
            EntityLayerType.ProjectWorkspace => scopedQuery.Where(x => x.s.ProjectWorkspaceId == layerId),
            EntityLayerType.ProjectSpace => scopedQuery.Where(x => x.s.Id == layerId),
            EntityLayerType.ProjectFolder => scopedQuery.Where(x => x.f != null && x.f.Id == layerId),
            EntityLayerType.ProjectList => scopedQuery.Where(x => x.l.Id == layerId),
            _ => scopedQuery.Where(_ => false)
        };

        var effectiveLayers = await scopedQuery
            .Where(x =>
                (!x.s.IsPrivate || entityAccess.Any(ea =>
                    ea.EntityId == x.s.Id &&
                    ea.EntityLayer == EntityLayerType.ProjectSpace &&
                    ea.WorkspaceMemberId == workspaceMemberId &&
                    ea.DeletedAt == null))
                &&
                (x.f == null || !x.f.IsPrivate || entityAccess.Any(ea =>
                    ea.EntityId == x.f.Id &&
                    ea.EntityLayer == EntityLayerType.ProjectFolder &&
                    ea.WorkspaceMemberId == workspaceMemberId &&
                    ea.DeletedAt == null))
                &&
                (!x.l.IsPrivate || entityAccess.Any(ea =>
                    ea.EntityId == x.l.Id &&
                    ea.EntityLayer == EntityLayerType.ProjectList &&
                    ea.WorkspaceMemberId == workspaceMemberId &&
                    ea.DeletedAt == null)))
            .Select(x => new
            {
                LayerId = !x.l.InheritStatus
                    ? x.l.Id
                    : x.f != null && !x.f.InheritStatus
                        ? x.f.Id
                        : x.s.Id,
                LayerType = !x.l.InheritStatus
                    ? EntityLayerType.ProjectList
                    : x.f != null && !x.f.InheritStatus
                        ? EntityLayerType.ProjectFolder
                        : EntityLayerType.ProjectSpace
            })
            .Distinct()
            .ToListAsync();

        if (effectiveLayers.Count == 0)
        {
            return await GetEffectiveStatuses(layerId, layerType);
        }

        var allowedLayerKeys = effectiveLayers
            .Select(x => $"{x.LayerId:N}:{(int)x.LayerType}")
            .ToHashSet();
        var layerIds = effectiveLayers.Select(x => x.LayerId).Distinct().ToList();

        var rawStatuses = await _unitOfWork.Set<Status>()
            .AsNoTracking()
            .Where(s => s.DeletedAt == null && s.LayerId.HasValue && layerIds.Contains(s.LayerId.Value))
            .OrderBy(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Color,
                s.Category,
                IsDefault = s.IsDefaultStatus,
                LayerId = s.LayerId!.Value,
                s.LayerType
            })
            .ToListAsync();

        return rawStatuses
            .Where(s => allowedLayerKeys.Contains($"{s.LayerId:N}:{(int)s.LayerType}"))
            .Select(s => new StatusDto(s.Id, s.Name, s.Color, s.Category, s.IsDefault))
            .ToList();
    }

    private async Task FetchAssignees(List<TaskDto> tasks)
    {
        var taskIds = tasks.Select(t => t.Id).ToList();
        if (!taskIds.Any()) return;

        const string sql = @"
            SELECT ta.task_id AS TaskId, u.id AS Id, u.name AS Name, NULL AS AvatarUrl
            FROM   task_assignments ta
            JOIN   workspace_members wm ON ta.workspace_member_id = wm.id
            JOIN   users u             ON wm.user_id = u.id
            WHERE  ta.task_id = ANY(@TaskIds)
              AND  ta.deleted_at IS NULL";

        var assignments = await _unitOfWork.QueryAsync<dynamic>(sql, new { TaskIds = taskIds.ToArray() });

        var assignmentLookup = assignments
            .GroupBy(a => (Guid)a.taskid)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => new AssigneeDto((Guid)a.id, (string)a.name, (string?)a.avatarurl)).ToList()
            );

        foreach (var task in tasks)
        {
            if (assignmentLookup.TryGetValue(task.Id, out var assignees))
            {
                task.Assignees = assignees;
            }
        }
    }

    // Status resolution with Dapper, using correct snake_case column names.
    // Layer "bubble-up" is handled by IStatusResolver.ResolveEffectiveLayer.
    private async Task<IEnumerable<StatusDto>> GetEffectiveStatuses(Guid layerId, EntityLayerType layerType)
    {
        var effectiveLayer = await _statusResolver.ResolveEffectiveLayer(layerId, layerType);

        const string sql = @"
            SELECT id, name, color, category, is_default_status AS IsDefault
            FROM   statuses
            WHERE  layer_id   = @LayerId
              AND  layer_type  = @LayerType
              AND  deleted_at IS NULL
            ORDER BY created_at";

        return await _unitOfWork.QueryAsync<StatusDto>(sql, new
        {
            LayerId   = effectiveLayer.LayerId,
            LayerType = effectiveLayer.LayerType.ToString()
        });
    }
}
