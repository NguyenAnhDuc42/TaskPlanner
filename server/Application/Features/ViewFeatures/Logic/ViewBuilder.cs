using Application.Contract.Common;
using Application.Contract.StatusContract;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using server.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Application.Features.ViewFeatures.GetViewData;

namespace Application.Features.ViewFeatures.Logic;

public class ViewBuilder
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStatusResolver _statusResolver;
    private readonly ICurrentUserService _currentUserService;

    public ViewBuilder(
        IUnitOfWork unitOfWork,
        IStatusResolver statusResolver,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _statusResolver = statusResolver;
        _currentUserService = currentUserService;
    }

    public async Task<BaseViewResult> Build(Guid layerId, EntityLayerType layerType, ViewDefinition view)
    {
        return view.ViewType switch
        {
            ViewType.List  => await BuildListView(layerId, layerType),
            ViewType.Board => await BuildBoardView(layerId, layerType),
            ViewType.Doc   => await BuildDocView(layerId, layerType),
            _              => throw new NotSupportedException($"ViewType {view.ViewType} is not supported yet.")
        };
    }

    // ──────────────────────────────────────────────────────────
    // View-specific builders
    // ──────────────────────────────────────────────────────────

    private async Task<TaskListViewResult> BuildListView(Guid layerId, EntityLayerType layerType)
    {
        var (tasks, statuses) = await GetTasksAndStatuses(layerId, layerType);
        return new TaskListViewResult(tasks, statuses);
    }

    private async Task<TasksBoardViewResult> BuildBoardView(Guid layerId, EntityLayerType layerType)
    {
        var (tasks, statuses) = await GetTasksAndStatuses(layerId, layerType);
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
        Guid layerId, EntityLayerType layerType)
    {
        // Delegate to TaskSql which already has the correct SQL per layer type,
        // including access-control joins (entity_access / workspace_members / private flags).
        var userId = _currentUserService.CurrentUserId();
        var sql = TaskSql.GetSql(layerType);
        var tasks = (await _unitOfWork.QueryAsync<TaskDto>(sql, new { layerId, UserId = userId })).ToList();

        await FetchAssignees(tasks);

        var statuses = await GetEffectiveStatuses(layerId, layerType);
        return (tasks, statuses);
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
            WHERE  ta.task_id IN @TaskIds
              AND  ta.deleted_at IS NULL";

        var assignments = await _unitOfWork.QueryAsync<dynamic>(sql, new { TaskIds = taskIds });

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
