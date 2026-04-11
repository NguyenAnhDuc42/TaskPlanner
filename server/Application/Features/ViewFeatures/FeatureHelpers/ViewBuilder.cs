using Application.Interfaces.Data;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;
using Application.Features.ViewFeatures.GetViewData;
using Application.Helpers;
using Application.Features.TaskFeatures.SelfManagement;
using Dapper;

namespace Application.Features.ViewFeatures.FeatureHelpers;

public class ViewBuilder
{
    private readonly IDataBase _db;

    public ViewBuilder(IDataBase db)
    {
        _db = db;
    }

    public async Task<BaseViewResult> Build(
        Guid layerId,
        EntityLayerType layerType,
        ViewDefinition view)
    {
        return view.ViewType switch
        {
            ViewType.List  => await BuildListView(layerId, layerType),
            ViewType.Board => await BuildBoardView(layerId, layerType),
            _              => throw new NotSupportedException($"ViewType {view.ViewType} is not supported yet.")
        };
    }

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

    private async Task<(IEnumerable<TaskDto> Tasks, IEnumerable<StatusDto> Statuses)> GetTasksAndStatuses(
        Guid layerId, EntityLayerType layerType)
    {
        var sql = TaskSql.GetSql(layerType);
        // EntityAccess removed: No WorkspaceMemberId check in SQL
        var tasksResult = await _db.Connection.QueryAsync<TaskDto>(sql, new { layerId });
        var tasks = tasksResult.ToList();

        await FetchAssignees(tasks);

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
            EntityLayerType.ProjectSpace => await _db.Connection.QuerySingleOrDefaultAsync<Guid?>(
                "SELECT project_workspace_id FROM project_spaces WHERE id = @Id", new { Id = layerId }),
            EntityLayerType.ProjectFolder => await _db.Connection.QuerySingleOrDefaultAsync<Guid?>(
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

        return await _db.Connection.QueryAsync<StatusDto>(sql, new { WorkspaceId = workspaceId });
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

        var assignments = await _db.Connection.QueryAsync<dynamic>(sql, new { TaskIds = taskIds.ToArray() });

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
