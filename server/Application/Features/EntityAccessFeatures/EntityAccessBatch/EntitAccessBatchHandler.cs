using Microsoft.EntityFrameworkCore;


namespace Application;

public class EntityAccessBatchHandler(TaskPlanDbContext db,WorkspaceContext workspaceContext): ICommandHandler<EntityAccessBatchCommand>,IAuthorizedWorkspaceRequest
{
   
    public async Task<Result> Handle(EntityAccessBatchCommand request, CancellationToken cancellationToken)
    {
        var space = await db.ProjectSpaces.FirstOrDefaultAsync(x => x.Id == request.SpaceId && x.ProjectWorkspaceId == workspaceContext.workspaceId, cancellationToken);
        if (space is null) return Result.Failure(Error.NotFound("Space.NotFound", "Space not found"));

        var deleteAccess= request.Rows.Where(r => r.Action == RowAction.Delete).ToList();
        var createAccess= request.Rows.Where(r => r.Action == RowAction.Create).ToList();
        var updateAccess= request.Rows.Where(r => r.Action == RowAction.Update).ToList(); 

        var affectedIds = updateAccess.Select(r => r.MemberId)
            .Concat(deleteAccess.Select(r => r.MemberId))
            .ToList();

        var existingAccesses = await db.EntityAccesses
            .Where(a => a.ProjectSpaceId == request.SpaceId && affectedIds.Contains(a.WorkspaceMemberId) && a.DeletedAt == null)
            .ToListAsync(cancellationToken);
        
        var existingLookUp = existingAccesses.ToDictionary(a => a.WorkspaceMemberId);

        foreach(var row in deleteAccess)
        {
            if(!existingLookUp.TryGetValue(row.MemberId, out var entity)) 
                return Result.Failure(Error.Validation("Access.NotFound", $"Cannot delete access for member {row.MemberId} because it does not exist."));
            
            entity.SoftDelete();
        }

        foreach(var row in updateAccess)
        {
            if(!existingLookUp.TryGetValue(row.MemberId, out var entity)) 
                return Result.Failure(Error.Validation("Access.NotFound", $"Cannot update access for member {row.MemberId} because it does not exist."));
                
            entity.Update(row.AccessLevel);
        }

        // Validate that all createAccess members actually belong to the workspace
        if (createAccess.Count > 0)
        {
            var createMemberIds = createAccess.Select(r => r.MemberId).ToList();
            var validWorkspaceMembers = await db.WorkspaceMembers
                .Where(wm => wm.ProjectWorkspaceId == workspaceContext.workspaceId && createMemberIds.Contains(wm.Id))
                .Select(wm => wm.Id)
                .ToListAsync(cancellationToken);

            var invalidMembers = createMemberIds.Except(validWorkspaceMembers).ToList();
            if (invalidMembers.Count > 0)
                return Result.Failure(Error.Validation("Member.Invalid", $"Members {string.Join(", ", invalidMembers)} do not exist in this workspace."));
        }

        var newAccess = createAccess
            .Select(row => EntityAccess.Create(
                workspaceContext.workspaceId,
                row.MemberId,
                request.SpaceId,
                projectFolderId: null,
                projectTaskId: null,
                row.AccessLevel,
                workspaceContext.CurrentMember.Id))
            .ToList();

        if (newAccess.Count > 0)
        {
            await db.EntityAccesses.AddRangeAsync(newAccess, cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

