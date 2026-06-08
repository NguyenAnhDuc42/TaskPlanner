using Microsoft.EntityFrameworkCore;
using Domain;

namespace Application;

public class UpdateTaskAssigneesHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    RealtimeService realtime
) : ICommandHandler<UpdateTaskAssigneesCommand>
{
    public async Task<Result> Handle(UpdateTaskAssigneesCommand request, CancellationToken ct)
    {
        var task = await db.ProjectTasks
            .Include(t => t.Assignees)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, ct);

        if (task == null) return Result.Failure(TaskError.NotFound);

        var memberIdsToProcess = request.Changes.Select(c => c.MemberId).ToList();

        // Validate members exist in workspace and are active
        var activeMemberIds = await db.WorkspaceMembers
            .Where(wm => wm.ProjectWorkspaceId == task.ProjectWorkspaceId && memberIdsToProcess.Contains(wm.Id) && wm.DeletedAt == null)
            .Select(wm => wm.Id)
            .ToListAsync(ct);

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
                toAdd.Add(TaskAssignment.Create(task.Id, change.MemberId, context.CurrentMember.Id));
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

        await db.SaveChangesAsync(ct);

        // Transactional SignalR broadcasts
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
            await realtime.NotifyEntitiesUpdatedAsync(context.WorkspaceId, updatePayload, ct);
        }

        if (deletedAssigneeIds.Count > 0)
        {
            var deletePayload = new EntityBatchDelete
            {
                AssigneeIds = deletedAssigneeIds
            };
            await realtime.NotifyEntitiesDeletedAsync(context.WorkspaceId, deletePayload, ct);
        }

        return Result.Success();
    }
}
