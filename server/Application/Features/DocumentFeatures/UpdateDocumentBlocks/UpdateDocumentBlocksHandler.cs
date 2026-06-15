using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application;

public class UpdateDocumentBlocksHandler(TaskPlanDbContext db, WorkspaceContext context, PermissionService permissionService, RealtimeService realtimeService) : ICommandHandler<UpdateDocumentBlocksCommand>
{
    public async Task<Result> Handle(UpdateDocumentBlocksCommand request, CancellationToken cancellationToken)
    {
        var docCreatorId = await db.Documents
            .Where(d => d.Id == request.DocumentId && d.DeletedAt == null)
            .Select(d => d.CreatorId)
            .FirstOrDefaultAsync(cancellationToken);

        if (docCreatorId == null) return Result.Failure(Error.NotFound("Document.NotFound", "Document not found"));

        var spaceId = await db.ProjectSpaces
            .Where(s => s.DefaultDocumentId == request.DocumentId)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? await db.ProjectTasks
                .Where(t => t.DefaultDocumentId == request.DocumentId)
                .Select(t => (Guid?)t.ProjectSpaceId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? await db.EntityAssetLinks
                .Where(l => l.AssetId == request.DocumentId && l.AssetType == AssetType.Document && l.ProjectSpaceId != null)
                .Select(l => l.ProjectSpaceId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? Guid.Empty;

        var hasAccess = await permissionService.VerifyAsync(Role.Member, spaceId, AccessLevel.Editor, docCreatorId, cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var incomingBlocks = request.Blocks;

        // 1. CATEGORIZE: Split the incoming list into three buckets
        var toDeleteIds = incomingBlocks
            .Where(b => b.IsDeleted && b.Id.HasValue && b.Id != Guid.Empty)
            .Select(b => b.Id!.Value)
            .ToList();

        var toUpdateItems = incomingBlocks
            .Where(b => !b.IsDeleted && b.Id.HasValue && b.Id != Guid.Empty)
            .ToList();

        var toAddItems = incomingBlocks
            .Where(b => !b.IsDeleted && (!b.Id.HasValue || b.Id == Guid.Empty))
            .ToList();

        // 2. FETCH: Get all existing entities needed for Update/Delete in ONE query
        var idsToFetch = toDeleteIds.Concat(toUpdateItems.Select(x => x.Id!.Value)).Distinct().ToList();
        var existingBlocksMap = await db.DocumentBlocks
            .Where(b => idsToFetch.Contains(b.Id))
            .ToDictionaryAsync(b => b.Id, cancellationToken);

        // 3. EXECUTE DELETES
        var blocksToRemove = toDeleteIds
            .Where(id => existingBlocksMap.ContainsKey(id))
            .Select(id => existingBlocksMap[id])
            .ToList();
        
        if (blocksToRemove.Any()) db.DocumentBlocks.RemoveRange(blocksToRemove);

        // 4. EXECUTE UPDATES
        foreach (var item in toUpdateItems)
        {
            if (existingBlocksMap.TryGetValue(item.Id!.Value, out var existing))
            {
                if (item.Content is not null) existing.UpdateContent(item.Content);
                if (item.OrderKey is not null) existing.UpdateOrderKey(item.OrderKey);
                if (item.BlockType.HasValue) existing.UpdateType(item.BlockType.Value);
            }
        }

        // 5. EXECUTE ADDS
        var newBlocks = toAddItems.Select(item => DocumentBlock.Create(
            context.WorkspaceId, 
            request.DocumentId, 
            item.BlockType ?? BlockType.Paragraph, 
            item.Content ?? string.Empty, 
            item.OrderKey ?? string.Empty, 
            context.CurrentMember.Id
        )).ToList();

        if (newBlocks.Any()) db.DocumentBlocks.AddRange(newBlocks);

        // 6. SAVE: One trip to the DB for everything
        var affected = await db.SaveChangesAsync(cancellationToken);

        if (affected > 0)
        {
            var updatedRecords = existingBlocksMap.Values
                .Where(b => toUpdateItems.Any(i => i.Id == b.Id))
                .Concat(newBlocks)
                .Select(b => new DocumentBlockRecord { Id = b.Id, Type = b.Type, Content = b.Content, OrderKey = b.OrderKey })
                .ToList();

            if (updatedRecords.Any())
            {
                await realtimeService.NotifyEntitiesUpdatedAsync(
                    context.WorkspaceId,
                    new EntityBatchUpdate { DocumentBlocks = updatedRecords },
                    cancellationToken);
            }

            if (toDeleteIds.Any())
            {
                await realtimeService.NotifyEntitiesDeletedAsync(
                    context.WorkspaceId,
                    new EntityBatchDelete { DocumentBlockIds = toDeleteIds },
                    cancellationToken);
            }
        }

        return Result.Success();
    }
}


