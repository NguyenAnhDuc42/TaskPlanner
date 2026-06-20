using Microsoft.EntityFrameworkCore;
namespace Application;

public class MoveItemHandler(TaskPlanDbContext db, WorkspaceContext context, RealtimeService realtime, PermissionService permissionService) : ICommandHandler<MoveItemCommand>
{
    public async Task<Result> Handle(MoveItemCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin, cancellationToken: cancellationToken);
        if (!hasAccess) 
            return Result.Failure(MemberError.DontHavePermission);

        var newOrderKey = request.NewOrderKey ?? await ResolveOrderKey(request, cancellationToken);

        var result = request.ItemType switch
        {
            EntityLayerType.ProjectSpace => await MoveSpace(request.ItemId, newOrderKey, cancellationToken),
            EntityLayerType.ProjectFolder => await MoveFolder(request.ItemId, request.TargetParentId, newOrderKey, cancellationToken),
            EntityLayerType.ProjectTask => await MoveTask(request.ItemId, request.TargetParentId, newOrderKey, cancellationToken),
            _ => Result.Failure(Error.Validation("Item.UnknownType", $"Unknown item type: {request.ItemType}"))
        };

        if (result.IsSuccess)
        {
            await realtime.NotifyWorkspaceAsync(context.WorkspaceId, "HierarchyChanged", new { 
                request.ItemId, 
                request.ItemType, 
                request.TargetParentId, 
                SourceParentId = request.SourceParentId,
                NewOrderKey = newOrderKey,
                SenderId = context.CurrentMember.UserId
            }, cancellationToken);
        }

        return result;
    }

    private async Task<string> ResolveOrderKey(MoveItemCommand request, CancellationToken cancellationToken)
    {
        if (request.PreviousItemOrderKey != null && request.NextItemOrderKey != null)
        {
            if (string.Compare(request.PreviousItemOrderKey, request.NextItemOrderKey, StringComparison.Ordinal) >= 0)
                return FractionalIndex.After(request.PreviousItemOrderKey);

            return FractionalIndex.Between(request.PreviousItemOrderKey, request.NextItemOrderKey);
        }

        if (request.PreviousItemOrderKey != null) return FractionalIndex.After(request.PreviousItemOrderKey);
        if (request.NextItemOrderKey != null) return FractionalIndex.Before(request.NextItemOrderKey);

        var maxKey = await GetMaxOrderKey(request, cancellationToken);
        return FractionalIndex.SafeAfter(maxKey);
    }

    private async Task<string?> GetMaxOrderKey(MoveItemCommand request, CancellationToken cancellationToken)
    {
        return request.ItemType switch
        {
            EntityLayerType.ProjectSpace => await db.ProjectSpaces
                                 .AsNoTracking()
                                 .Where(s => s.ProjectWorkspaceId == request.TargetParentId && s.DeletedAt == null)
                                 .MaxAsync(s => s.OrderKey, cancellationToken),

            EntityLayerType.ProjectFolder => await db.ProjectFolders
                                 .AsNoTracking()
                                 .Where(f => f.ProjectSpaceId == request.TargetParentId.GetValueOrDefault() && f.DeletedAt == null)
                                 .MaxAsync(f => f.OrderKey, cancellationToken),

            EntityLayerType.ProjectTask => await db.ProjectTasks
                                 .AsNoTracking()
                                 .Where(t => (request.TargetParentId.HasValue && t.ProjectFolderId == request.TargetParentId && t.DeletedAt == null) ||
                                            (!request.TargetParentId.HasValue && t.ProjectSpaceId == request.ItemId && t.ProjectFolderId == null && t.DeletedAt == null))
                                 .MaxAsync(t => t.OrderKey, cancellationToken),
            _ => null
        };
    }

    private async Task<Result> MoveSpace(Guid spaceId, string newOrderKey, CancellationToken cancellationToken)
    {
        var affected = await db.ProjectSpaces
            .Where(s => s.Id == spaceId)
            .ExecuteUpdateAsync(u => u.SetProperty(s => s.OrderKey, newOrderKey)
                                      .SetProperty(s => s.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);

        return affected > 0 ? Result.Success() : Result.Failure(SpaceError.NotFound);
    }

    private async Task<Result> MoveFolder(Guid folderId, Guid? newSpaceId, string newOrderKey, CancellationToken cancellationToken)
    {
        var affected = await db.ProjectFolders
            .Where(f => f.Id == folderId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(f => f.ProjectSpaceId, f => newSpaceId ?? f.ProjectSpaceId)
                .SetProperty(f => f.OrderKey, newOrderKey)
                .SetProperty(f => f.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);

        return affected > 0 ? Result.Success() : Result.Failure(FolderError.NotFound);
    }

    private async Task<Result> MoveTask(Guid taskId, Guid? targetParentId, string newOrderKey, CancellationToken cancellationToken)
    {
        Guid? resolvedSpaceId = null;
        Guid? resolvedFolderId = null;

        if (targetParentId.HasValue)
        {
            var targetGuid = targetParentId.Value;
            var targetInfo = await db.ProjectSpaces
                .Where(s => s.Id == targetGuid)
                .Select(s => new { Id = s.Id, Type = "ProjectSpace" })
                .Union(db.ProjectFolders.Where(f => f.Id == targetGuid).Select(f => new { Id = f.ProjectSpaceId, Type = "ProjectFolder" }))
                .FirstOrDefaultAsync(cancellationToken);

            if (targetInfo == null) return Result.Failure(Error.Validation("MoveTask.InvalidTarget", "Target parent not found."));
            
            if (targetInfo.Type == "ProjectSpace") resolvedSpaceId = targetGuid;
            else { resolvedFolderId = targetGuid; resolvedSpaceId = targetInfo.Id; }
        }

        if (resolvedSpaceId == null) return Result.Failure(Error.Validation("MoveTask.InvalidTarget", "No valid target space resolved."));

        var affected = await db.ProjectTasks
            .Where(t => t.Id == taskId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(t => t.ProjectSpaceId, resolvedSpaceId)
                .SetProperty(t => t.ProjectFolderId, resolvedFolderId)
                .SetProperty(t => t.OrderKey, newOrderKey)
                .SetProperty(t => t.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);

        return affected > 0 ? Result.Success() : Result.Failure(Error.NotFound("Task.NotFound", "Task not found"));
    }
}



