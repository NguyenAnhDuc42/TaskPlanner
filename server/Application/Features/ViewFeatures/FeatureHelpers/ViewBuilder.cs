using Application.Contract.StatusContract;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;
using Application.Contract.Common;
using Application.Features.ViewFeatures.GetViewData;
using Application.Helpers;
using Domain.Entities.Relationship;

namespace Application.Features.ViewFeatures.FeatureHelpers;

public class ViewBuilder
{
    private readonly IUnitOfWork _unitOfWork;

    public ViewBuilder(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
            _              => throw new NotSupportedException($"ViewType {view.ViewType} is not supported yet.")
        };
    }

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


    private async Task<(IEnumerable<TaskDto> Tasks, IEnumerable<StatusDto> Statuses)> GetTasksAndStatuses(
        Guid layerId, EntityLayerType layerType, Guid workspaceMemberId)
    {
        var sql = TaskSql.GetSql(layerType);
        var tasksResult = await _unitOfWork.QueryAsync<TaskDto>(sql, new { layerId, WorkspaceMemberId = workspaceMemberId });
        var tasks = tasksResult.ToList();

        await FetchAssignees(tasks);

        // Statuses are always owned by the Space
        var spaceId = await ResolveSpaceId(layerId, layerType);
        var statuses = spaceId.HasValue
            ? await GetStatusesForSpace(spaceId.Value)
            : Enumerable.Empty<StatusDto>();

        return (tasks, statuses);
    }

    /// <summary>
    /// Resolves the SpaceId for a given layer. Uses Dapper for non-Space layers.
    /// </summary>
    private async Task<Guid?> ResolveSpaceId(Guid layerId, EntityLayerType layerType)
    {
        return layerType switch
        {
            EntityLayerType.ProjectSpace => layerId,
            EntityLayerType.ProjectFolder => await _unitOfWork.QuerySingleOrDefaultAsync<Guid?>(
                "SELECT project_space_id FROM project_folders WHERE id = @Id", new { Id = layerId }),
            EntityLayerType.ProjectWorkspace => null, // No single space at workspace level
            _ => null
        };
    }

    private async Task<IEnumerable<StatusDto>> GetStatusesForSpace(Guid spaceId)
    {
        const string sql = @"
            SELECT s.id, s.name, s.color, s.category, s.is_default_status AS IsDefault
            FROM   statuses s
            JOIN   workflows w ON s.workflow_id = w.id
            WHERE  w.project_space_id = @SpaceId
              AND  s.deleted_at IS NULL
            ORDER BY s.created_at";

        return await _unitOfWork.QueryAsync<StatusDto>(sql, new { SpaceId = spaceId });
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
}
