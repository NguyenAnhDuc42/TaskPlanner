using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;
using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.StatusManagement.GetWorkspaceStatuses;

public class GetWorkspaceStatusesHandler : IQueryHandler<GetWorkspaceStatusesQuery, List<StatusDto>>
{
    private readonly IDataBase _db;

    public GetWorkspaceStatusesHandler(IDataBase db)
    {
        _db = db;
    }

    public async Task<Result<List<StatusDto>>> Handle(GetWorkspaceStatusesQuery request, CancellationToken ct)
    {
        // 1. Resolve Hierarchy Workflow
        var workflow = await ResolveHierarchyWorkflowAsync(request, ct);

        if (workflow == null)
            return Result.Failure<List<StatusDto>>(Error.NotFound("Workflow.NotFound", "No lifecycle workflow found for the given context."));

        // 2. Fetch Statuses
        var statuses = await _db.Statuses
            .ByWorkflow(workflow.Id)
            .WhereNotDeleted()
            .OrderBy(s => s.CreatedAt)
            .Select(s => new StatusDto(
                s.Id,
                s.Name,
                s.Color,
                s.Category
            ))
            .ToListAsync(ct);

        return Result.Success(statuses);
    }

    private async Task<Workflow?> ResolveHierarchyWorkflowAsync(GetWorkspaceStatusesQuery request, CancellationToken ct)
    {
        // Try Folder first
        if (request.FolderId.HasValue)
        {
            var folderWorkflow = await _db.Workflows
                .ByFolder(request.FolderId.Value)
                .WhereNotDeleted()
                .FirstOrDefaultAsync(ct);
            if (folderWorkflow != null) return folderWorkflow;
        }

        // Try Space second
        if (request.SpaceId.HasValue)
        {
            var spaceWorkflow = await _db.Workflows
                .BySpace(request.SpaceId.Value)
                .WhereNotDeleted()
                .FirstOrDefaultAsync(ct);
            if (spaceWorkflow != null) return spaceWorkflow;
        }

        // Fallback to Workspace
        return await _db.Workspaces
            .ById(request.WorkspaceId)
            .Join(_db.Workflows.Where(w => w.SpaceId == null && w.FolderId == null && w.DeletedAt == null),
                  w => w.Id,
                  wf => wf.ProjectWorkspaceId,
                  (w, wf) => wf)
            .FirstOrDefaultAsync(ct);
    }
}
