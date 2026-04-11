using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;
using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.StatusManagement.SyncStatuses;

public class SyncStatusesHandler : ICommandHandler<SyncStatusesCommand>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;

    public SyncStatusesHandler(IDataBase db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(SyncStatusesCommand request, CancellationToken ct)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var workflow = await _db.Workflows
            .ById(request.WorkflowId)
            .FirstOrDefaultAsync(ct);

        if (workflow == null) return Result.Failure(Error.NotFound("Workflow.NotFound", $"Workflow {request.WorkflowId} not found"));

        var existingStatuses = await _db.Statuses
            .ByWorkflow(request.WorkflowId)
            .WhereNotDeleted()
            .ToListAsync(ct);

        foreach (var item in request.Statuses)
        {
            if (item.Id == null || item.Id == Guid.Empty)
            {
                if (item.IsDeleted) continue;

                var newStatus = Status.Create(
                    workflow.ProjectWorkspaceId,
                    request.WorkflowId,
                    item.Name,
                    item.Color,
                    item.Category,
                    currentUserId
                );
                await _db.Statuses.AddAsync(newStatus, ct);
            }
            else
            {
                var status = existingStatuses.FirstOrDefault(s => s.Id == item.Id);
                if (status == null) continue;

                if (item.IsDeleted)
                {
                    status.SoftDelete();
                }
                else
                {
                    status.UpdateDetails(item.Name, item.Color, item.Category);
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
