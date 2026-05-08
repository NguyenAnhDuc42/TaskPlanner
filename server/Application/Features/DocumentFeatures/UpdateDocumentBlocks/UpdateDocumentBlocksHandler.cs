using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;
using Application.Helpers;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.DocumentFeatures;

public class UpdateDocumentBlocksHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<UpdateDocumentBlocksCommand>
{
    public async Task<Result> Handle(UpdateDocumentBlocksCommand request, CancellationToken ct)
    {
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
            .ToDictionaryAsync(b => b.Id, ct);

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
            context.workspaceId, 
            request.DocumentId, 
            item.BlockType ?? BlockType.Paragraph, 
            item.Content ?? string.Empty, 
            item.OrderKey ?? string.Empty, 
            context.CurrentMember.Id
        )).ToList();

        if (newBlocks.Any()) db.DocumentBlocks.AddRange(newBlocks);

        // 6. SAVE: One trip to the DB for everything
        await db.SaveChangesAsync(ct);

        return Result.Success();
    }
}
