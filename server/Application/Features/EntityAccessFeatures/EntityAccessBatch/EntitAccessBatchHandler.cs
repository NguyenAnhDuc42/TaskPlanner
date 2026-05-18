using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;


namespace Application.Features.EntityAccessFeatures;

public class EntityAccessBatchHandler(IDataBase db,WorkspaceContext workspaceContext): ICommandHandler<EntityAccessBatchCommand>,IAuthorizedWorkspaceRequest
{
   
    public async Task<Result> Handle(EntityAccessBatchCommand request, CancellationToken cancellationToken)
    {
        var space = await db.Spaces.FirstOrDefaultAsync(x => x.Id == request.SpaceId && x.ProjectWorkspaceId == workspaceContext.workspaceId, cancellationToken);
        if (space is null) return Result.Failure(Error.NotFound("Space.NotFound", "Space not found"));

        var deleteAccess= request.Rows.Where(r => r.Action == RowAction.Delete).ToList();
        var createAccess= request.Rows.Where(r => r.Action == RowAction.Create).ToList();
        var updateAccess= request.Rows.Where(r => r.Action == RowAction.Update).ToList(); 

        var affectedIds = updateAccess.Select(r => r.MemberId)
            .Concat(deleteAccess.Select(r => r.MemberId))
            .ToList();

        var existingAccesses = await db.Access
            .Where(a => a.ProjectSpaceId == request.SpaceId && affectedIds.Contains(a.WorkspaceMemberId) && a.DeletedAt == null)
            .ToListAsync(cancellationToken);
        
        var existingLookUp = existingAccesses.ToDictionary(a => a.WorkspaceMemberId);

        foreach(var row in deleteAccess)
        {
            if(!existingLookUp.TryGetValue(row.MemberId, out var entity)) continue;
            entity.SoftDelete();
        }

        foreach(var row in updateAccess)
        {
            if(!existingLookUp.TryGetValue(row.MemberId, out var entity)) continue;
            entity.Update(row.AccessLevel);
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
            await db.Access.AddRangeAsync(newAccess, cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}