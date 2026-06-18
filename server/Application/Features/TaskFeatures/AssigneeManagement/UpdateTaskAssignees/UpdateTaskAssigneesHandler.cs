using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Application;

public class UpdateTaskAssigneesHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtime,
    ILogger<UpdateTaskAssigneesHandler> logger
) : ICommandHandler<UpdateTaskAssigneesCommand>
{
    public async Task<Result> Handle(UpdateTaskAssigneesCommand request, CancellationToken cancellationToken )
    {
        var task = await db.ProjectTasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId && t.DeletedAt == null, cancellationToken);
            
        if (task == null) return Result.Failure(TaskError.NotFound);

        var hasAccess = await permissionService.VerifyAsync(Role.Member,task.ProjectSpaceId,AccessLevel.Editor, task.CreatorId, cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var memberIdsToProcess = request.Changes.Select(c => c.MemberId).ToList();


        var activeMemberIds = await db.WorkspaceMembers
            .Where(wm => memberIdsToProcess.Contains(wm.Id) && wm.DeletedAt == null)
            .Select(wm => wm.Id)
            .ToListAsync(cancellationToken);

        var toAdd = new List<TaskAssignment>();
        var memberIdsToRemove = new List<Guid>();

        foreach (var change in request.Changes)
        {
            if (!activeMemberIds.Contains(change.MemberId)) continue;

            if (change.IsDelete)
            {
                memberIdsToRemove.Add(change.MemberId);
            }
            else if (!task.Assignees.Any(a => a.WorkspaceMemberId == change.MemberId))
            {
                toAdd.Add(TaskAssignment.Create(task.Id, change.MemberId, workspaceContext.CurrentMember.Id));
            }
        }

        var deletedAssigneeIds = new List<Guid>();
        if (memberIdsToRemove.Count > 0)
        {
            deletedAssigneeIds = task.Assignees
                .Where(a => memberIdsToRemove.Contains(a.WorkspaceMemberId))
                .Select(a => a.Id)
                .ToList();

            task.RemoveAsignees(memberIdsToRemove);
        }

        if (toAdd.Count > 0)
        {
            task.AddAsignees(toAdd);
        }

        await db.SaveChangesAsync(cancellationToken);

        if (toAdd.Count > 0)
        {
            var updatePayload = new EntityBatchUpdate
            {
                Assignees = toAdd.Select(a => new AssigneeRecord
                {
                    Id = a.Id,
                    TaskId = a.ProjectTaskId,
                    WorkspaceMemberId = a.WorkspaceMemberId
                }).ToList()
            };
            _ = realtime
            .NotifyEntitiesUpdatedAsync(workspaceContext.WorkspaceId, updatePayload, default)
            .ContinueWith(t => logger.LogError(t.Exception, "Failed to send realtime notification for added assignees"), TaskContinuationOptions.OnlyOnFaulted);
        }

        if (deletedAssigneeIds.Count > 0)
        {
            var deletePayload = new EntityBatchDelete
            {
                AssigneeIds = deletedAssigneeIds
            };
            _ = realtime
            .NotifyEntitiesDeletedAsync(workspaceContext.WorkspaceId, deletePayload, default)
            .ContinueWith(t => logger.LogError(t.Exception, "Failed to send realtime notification for deleted assignees"), TaskContinuationOptions.OnlyOnFaulted);
        }

        return Result.Success();
    }
}
