using Microsoft.EntityFrameworkCore;

namespace Application;

public class EntityAccessBatchHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService
) : ICommandHandler<EntityAccessBatchCommand>, IAuthorizedWorkspaceRequest
{

    public async Task<Result> Handle(EntityAccessBatchCommand request, CancellationToken cancellationToken)
    {

        var space = await db.ProjectSpaces
            .AsNoTracking()
            .Where(s => s.Id == request.SpaceId && s.DeletedAt == null)
            .Select(s => new { s.Id, s.CreatorId })
            .FirstOrDefaultAsync(cancellationToken);
        
        if (space is null) return Result.Failure(Error.NotFound("Space.NotFound", "Space not found"));
        var hasAccess = await permissionService.VerifyAsync(Role.Member, space.Id, AccessLevel.Editor, space.CreatorId, cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var deleteAccess = request.Rows.Where(r => r.Action == RowAction.Delete).ToList();
        var createAccess = request.Rows.Where(r => r.Action == RowAction.Create).ToList();
        var updateAccess = request.Rows.Where(r => r.Action == RowAction.Update).ToList();

        var affectedRowIds = updateAccess.Select(r => r.Id ?? Guid.Empty)
            .Concat(deleteAccess.Select(r => r.Id ?? Guid.Empty))
            .Where(id => id != Guid.Empty)
            .ToList();

        var affectedMemberIds = updateAccess.Where(r => !r.Id.HasValue).Select(r => r.MemberId)
            .Concat(deleteAccess.Where(r => !r.Id.HasValue).Select(r => r.MemberId))
            .ToList();

        var existingAccesses = await db.EntityAccesses
            .Where(a => a.ProjectSpaceId == request.SpaceId &&
                        a.DeletedAt == null &&
                        (affectedRowIds.Contains(a.Id) || affectedMemberIds.Contains(a.WorkspaceMemberId)))
            .ToListAsync(cancellationToken);

        var existingLookUpById = existingAccesses.ToDictionary(a => a.Id);
        var existingLookUpByMember = existingAccesses.ToDictionary(a => a.WorkspaceMemberId);

        foreach (var row in deleteAccess)
        {
            EntityAccess? entity = null;
            if (row.Id.HasValue)
                existingLookUpById.TryGetValue(row.Id.Value, out entity);
            else
                existingLookUpByMember.TryGetValue(row.MemberId, out entity);

            if (entity is null)
                return Result.Failure(Error.Validation("Access.NotFound", "Cannot delete access because it does not exist."));

            // Hard-delete — entity_access is a relationship record, no audit trail needed
            db.EntityAccesses.Remove(entity);
        }

        foreach (var row in updateAccess)
        {
            EntityAccess? entity = null;
            if (row.Id.HasValue)
            {
                existingLookUpById.TryGetValue(row.Id.Value, out entity);
            }
            else
            {
                existingLookUpByMember.TryGetValue(row.MemberId, out entity);
            }

            if (entity is null)
                return Result.Failure(Error.Validation("Access.NotFound", "Cannot update access because it does not exist."));

            entity.Update(row.AccessLevel);
        }

        // Validate that all createAccess members actually belong to the workspace
        if (createAccess.Count > 0)
        {
            var createMemberIds = createAccess.Select(r => r.MemberId).ToList();
            var validWorkspaceMembers = await db.WorkspaceMembers
                .Where(wm => createMemberIds.Contains(wm.Id))
                .Select(wm => wm.Id)
                .ToListAsync(cancellationToken);

            var invalidMembers = createMemberIds.Except(validWorkspaceMembers).ToList();
            if (invalidMembers.Count > 0)
                return Result.Failure(Error.Validation("Member.Invalid", $"Members {string.Join(", ", invalidMembers)} do not exist in this workspace."));
        }

        // Create — always INSERT fresh (hard-delete means no orphaned soft-deleted records remain)
        var newAccess = createAccess
            .Select(row => EntityAccess.Create(
                workspaceContext.WorkspaceId,
                row.MemberId,
                request.SpaceId,
                projectFolderId: null,
                projectTaskId: null,
                row.AccessLevel,
                workspaceContext.CurrentMember.Id))
            .ToList();

        // Collect affected member IDs BEFORE save so we know who to notify
        var revokedMemberIds = deleteAccess.Select(r => r.MemberId).ToHashSet();
        var grantedMemberIds = createAccess.Select(r => r.MemberId).ToHashSet();

        if (newAccess.Count > 0)
            await db.EntityAccesses.AddRangeAsync(newAccess, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // Notify workspace for updated/new records (admins, the space view, etc.)
        var updatedRecords = newAccess.Select(EntityAccessRecord.FromDomain).ToList();
        if (updatedRecords.Count > 0)
        {
            _ = realtimeService.NotifyEntitiesUpdatedAsync(
                workspaceContext.WorkspaceId,
                new EntityBatchUpdate { EntityAccess = updatedRecords },
                default);
        }

        var allAffectedMemberIds = revokedMemberIds.Union(grantedMemberIds).ToList();
        if (allAffectedMemberIds.Count > 0)
        {
            var affectedUsers = await db.WorkspaceMembers
                .Where(m => allAffectedMemberIds.Contains(m.Id) && m.DeletedAt == null)
                .Select(m => new { m.Id, m.UserId })
                .ToListAsync(cancellationToken);

            foreach (var user in affectedUsers)
            {
                // Send the user their current active entity_access records for this space
                // so their frontend can update store + refetch hierarchy
                var userAccess = (await db.EntityAccesses
                    .Where(ea => ea.WorkspaceMemberId == user.Id
                              && ea.ProjectSpaceId == request.SpaceId
                              && ea.DeletedAt == null)
                    .ToListAsync(cancellationToken))
                    .Select(EntityAccessRecord.FromDomain)
                    .ToList();

                _ = realtimeService.NotifyUserAsync(
                    user.UserId,
                    "EntitiesUpdated",
                    new EntityBatchUpdate { EntityAccess = userAccess },
                    default);
            }
        }
        
        return Result.Success();
    }
}

