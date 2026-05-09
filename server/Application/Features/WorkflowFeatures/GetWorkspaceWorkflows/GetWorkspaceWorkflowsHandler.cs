using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;
using Dapper;

using Application.Helpers;

namespace Application.Features.WorkflowFeatures;

public class GetWorkspaceWorkflowsHandler(IDataBase db, WorkspaceContext workspaceContext) : IQueryHandler<GetWorkspaceWorkflowsQuery, List<WorkflowDto>>
{
    public async Task<Result<List<WorkflowDto>>> Handle(GetWorkspaceWorkflowsQuery request, CancellationToken ct)
    {
        const string sql = @"
            SELECT 
                w.id AS Id, w.name AS Name, w.project_space_id AS ProjectSpaceId, w.project_folder_id AS ProjectFolderId,
                s.id AS Id, s.name AS Name, s.color AS Color, s.category AS Category
            FROM workflows w
            LEFT JOIN statuses s ON w.id = s.workflow_id
            WHERE w.project_workspace_id = @WorkspaceId AND w.deleted_at IS NULL AND (s.deleted_at IS NULL OR s.id IS NULL)
            ORDER BY w.name, s.category, s.name;";

        var workflowDict = new Dictionary<Guid, WorkflowDto>();

        await db.Connection.QueryAsync<WorkflowDto, StatusDto, WorkflowDto>(
            sql,
            (workflow, status) =>
            {
                if (!workflowDict.TryGetValue(workflow.Id, out var workflowEntry))
                {
                    workflowEntry = workflow with { Statuses = new List<StatusDto>() };
                    workflowDict.Add(workflowEntry.Id, workflowEntry);
                }

                if (status != null)
                {
                    workflowEntry.Statuses.Add(status);
                }

                return workflowEntry;
            },
            new { WorkspaceId = workspaceContext.workspaceId },
            splitOn: "Id");

        return Result<List<WorkflowDto>>.Success(workflowDict.Values.ToList());
    }
}
