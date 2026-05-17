using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.WorkflowFeatures;

public class UpdateWorkflowStatusesHandler(IDataBase db, WorkspaceContext context) 
    : ICommandHandler<UpdateWorkflowStatusesCommand>
{
    public async Task<Result> Handle(UpdateWorkflowStatusesCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        var workflow = await db.Workflows
            .Include(w => w.Statuses)
            .FirstOrDefaultAsync(w => w.Id == request.WorkflowId, ct);

        if (workflow == null) return Result.Failure(WorkflowError.NotFound);

        foreach (var statusDto in request.Statuses)
        {
            switch (statusDto.Action)
            {
                case RowAction.Delete:
                    if (statusDto.Id.HasValue)
                    {
                        workflow.RemoveStatus(statusDto.Id.Value);
                    }
                    break;
                    
                case RowAction.Update:
                    if (statusDto.Id.HasValue)
                    {
                        var existing = workflow.Statuses.FirstOrDefault(s => s.Id == statusDto.Id.Value);
                        if (existing != null)
                        {
                            existing.UpdateName(statusDto.Name);
                            existing.UpdateColor(statusDto.Color!);
                            existing.UpdateCategory(statusDto.Category);
                            
                            if (statusDto.PreviousOrderKey != null || statusDto.NextOrderKey != null)
                            {
                                var resolvedKey = ResolveOrderKey(statusDto.PreviousOrderKey, statusDto.NextOrderKey);
                                existing.UpdateOrderKey(resolvedKey);
                            }
                        }
                    }
                    break;
                    
                case RowAction.Create:
                    var orderKey = (statusDto.PreviousOrderKey != null || statusDto.NextOrderKey != null)
                        ? ResolveOrderKey(statusDto.PreviousOrderKey, statusDto.NextOrderKey)
                        : null;
                    
                    var @new = Status.Create(
                        workflow.ProjectWorkspaceId, 
                        workflow.Id, 
                        statusDto.Name, 
                        statusDto.Color!, 
                        statusDto.Category, 
                        context.CurrentMember.Id,
                        orderKey);
                    
                    workflow.AddStatus(@new);
                    break;
            }
        }

        await db.SaveChangesAsync(ct);

        return Result.Success();
    }

    private static string ResolveOrderKey(string? prevKey, string? nextKey)
    {
        if (prevKey != null && nextKey != null)
        {
            if (string.Compare(prevKey, nextKey, StringComparison.Ordinal) >= 0)
                return FractionalIndex.After(prevKey);

            return FractionalIndex.Between(prevKey, nextKey);
        }

        if (prevKey != null) return FractionalIndex.After(prevKey);
        if (nextKey != null) return FractionalIndex.Before(nextKey);

        return FractionalIndex.Start();
    }
}
