using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;
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

        // Statuses are now owned by the Workspace
        var workspaceId = await ResolveWorkspaceId(layerId, layerType);
        var statuses = workspaceId.HasValue
            ? await GetStatusesForWorkspace(workspaceId.Value)
            : Enumerable.Empty<StatusDto>();

        return (tasks, statuses);
    }

    private async Task<Guid?> ResolveWorkspaceId(Guid layerId, EntityLayerType layerType)
    {
        return layerType switch
        {
            EntityLayerType.ProjectWorkspace => layerId,
            EntityLayerType.ProjectSpace => await _unitOfWork.QuerySingleOrDefaultAsync<Guid?>(
                "SELECT project_workspace_id FROM project_spaces WHERE id = @Id", new { Id = layerId }),
            EntityLayerType.ProjectFolder => await _unitOfWork.QuerySingleOrDefaultAsync<Guid?>(
                "SELECT s.project_workspace_id FROM project_folders f JOIN project_spaces s ON f.project_space_id = s.id WHERE f.id = @Id", new { Id = layerId }),
            _ => null
        };
    }

    private async Task<IEnumerable<StatusDto>> GetStatusesForWorkspace(Guid workspaceId)
    {
        const string sql = @"
            SELECT s.id, s.name, s.color, s.category
            FROM   statuses s
            JOIN   workflows w ON s.workflow_id = w.id
            WHERE  w.project_workspace_id = @WorkspaceId
              AND  s.deleted_at IS NULL
            ORDER BY s.created_at";

        return await _unitOfWork.QueryAsync<StatusDto>(sql, new { WorkspaceId = workspaceId });
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
