using Application.Interfaces.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Application.Helper;


public class DocumentHelper(IDataBase db) 
{
    public async Task EditDocument(
    Guid workspaceId, 
    Guid documentId, 
    List<(Guid? Id, string? Content, string? OrderKey, BlockType? BlockType, bool IsDeleted)> incomingBlocks, 
    Guid creatorId) 
    {
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
        .ToDictionaryAsync(b => b.Id);

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
            existing.UpdateContent(item.Content ?? string.Empty);
            existing.UpdateOrderKey(item.OrderKey ?? string.Empty);
            if (item.BlockType.HasValue) existing.UpdateType(item.BlockType.Value);
        }
    }

    // 5. EXECUTE ADDS
    var newBlocks = toAddItems.Select(item => DocumentBlock.Create(
        workspaceId, 
        documentId, 
        item.BlockType ?? BlockType.Paragraph, 
        item.Content ?? string.Empty, 
        item.OrderKey ?? string.Empty, 
        creatorId
    )).ToList();

    if (newBlocks.Any()) db.DocumentBlocks.AddRange(newBlocks);

    // 6. SAVE: One trip to the DB for everything
    await db.SaveChangesAsync();
    }
}