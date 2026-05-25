using Microsoft.EntityFrameworkCore;

namespace Application;

public class BatchUpdateFolderTasksHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, RealtimeService realtimeService) : ICommandHandler<BatchUpdateFolderTasksCommand>
{
    public async Task<Result> Handle(BatchUpdateFolderTasksCommand request, CancellationToken ct)
    {
        if (request.Updates == null || !request.Updates.Any())
        {
            return Result.Success();
        }

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            foreach (var update in request.Updates)
            {
                bool updateStatus = update.StatusId.HasValue;
                bool updatePriority = update.Priority != null;
                bool updateStartDate = update.StartDate != null;
                bool updateDueDate = update.DueDate != null;
                bool updateOrder = update.OrderKey != null;
                bool updateDeleted = update.IsDeleted.HasValue;

                var affected = await db.ProjectTasks
                    .Where(t => t.Id == update.Id && t.ProjectFolderId == request.FolderId && t.ProjectWorkspaceId == workspaceContext.workspaceId)
                    .ExecuteUpdateAsync(u => u
                        .SetProperty(t => t.StatusId, t => updateStatus ? (update.StatusId == Guid.Empty ? null : update.StatusId) : t.StatusId)
                        .SetProperty(t => t.Priority, t => updatePriority ? update.Priority : t.Priority)
                        .SetProperty(t => t.StartDate, t => updateStartDate ? update.StartDate : t.StartDate)
                        .SetProperty(t => t.DueDate, t => updateDueDate ? update.DueDate : t.DueDate)
                        .SetProperty(t => t.OrderKey, t => updateOrder ? update.OrderKey : t.OrderKey)
                        .SetProperty(t => t.DeletedAt, t => updateDeleted && update.IsDeleted!.Value ? DateTimeOffset.UtcNow : t.DeletedAt)
                        .SetProperty(t => t.UpdatedAt, DateTimeOffset.UtcNow), ct);

                if (affected == 0)
                {
                    return Result.Failure(Error.NotFound("Task.NotFound", $"Task {update.Id} not found in folder {request.FolderId}"));
                }
            }

            return Result.Success();
        }, ct);

        if (result.IsSuccess)
        {
            await realtimeService.NotifyWorkspaceAsync(workspaceContext.workspaceId, "FolderTasksBatchUpdated", new { FolderId = request.FolderId }, ct);
        }

        return result;
    }
}
