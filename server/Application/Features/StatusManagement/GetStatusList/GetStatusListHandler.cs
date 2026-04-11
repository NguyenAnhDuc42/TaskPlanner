using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;
using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.StatusManagement.GetStatusList;

public class GetStatusListHandler : IQueryHandler<GetStatusListQuery, List<StatusDto>>
{
    private readonly IDataBase _db;

    public GetStatusListHandler(IDataBase db)
    {
        _db = db;
    }

    public async Task<Result<List<StatusDto>>> Handle(GetStatusListQuery request, CancellationToken ct)
    {
        var workflowExist = await _db.Workflows
            .AsNoTracking()
            .ById(request.WorkflowId)
            .AnyAsync(ct);

        if (!workflowExist) return Result.Failure<List<StatusDto>>(Error.NotFound("Workflow.NotFound", $"Workflow {request.WorkflowId} not found"));

        var statuses = await _db.Statuses
            .ByWorkflow(request.WorkflowId)
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
}
