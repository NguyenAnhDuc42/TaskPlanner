using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.WorkflowFeatures.UpdateWorkflowStatuses;

public class UpdateWorkflowStatusesHandler(IDataBase db, WorkspaceContext context) 
    : ICommandHandler<UpdateWorkflowStatusesCommand>
{
    public async Task<Result> Handle(UpdateWorkflowStatusesCommand request, CancellationToken ct)
    {
        // 1. Permission Check
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        // 2. Load Workflow
        var workflow = await db.Workflows
            .Include(w => w.Statuses)
            .FirstOrDefaultAsync(w => w.Id == request.WorkflowId, ct);

        if (workflow == null) return Result.Failure(WorkflowError.NotFound);

        // 3. Process Statuses
        var incomingStatusIds = request.Statuses.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToList();
        var existingStatusIds = workflow.Statuses.Select(s => s.Id).ToList();

        // 3a. Remove deleted statuses
        var idsToRemove = existingStatusIds.Except(incomingStatusIds).ToList();
        foreach (var id in idsToRemove)
        {
            workflow.RemoveStatus(id);
        }

        // 3b. Update or Add
        foreach (var statusDto in request.Statuses)
        {
            if (statusDto.Id.HasValue)
            {
                var existing = workflow.Statuses.FirstOrDefault(s => s.Id == statusDto.Id.Value);
                if (existing != null)
                {
                    existing.UpdateDetails(statusDto.Name, statusDto.Color, statusDto.Category);
                }
            }
            else
            {
                var @new = Status.Create(
                    workflow.ProjectWorkspaceId, 
                    workflow.Id, 
                    statusDto.Name, 
                    statusDto.Color, 
                    statusDto.Category, 
                    context.CurrentMember.Id);
                
                workflow.AddStatus(@new);
            }
        }

        // 4. Validate Integrity
        try
        {
            workflow.ValidateIntegrity();
        }
        catch (Domain.Exceptions.BusinessRuleException)
        {
            return Result.Failure(WorkflowError.IntegrityViolation);
        }

        // 5. Persist
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
