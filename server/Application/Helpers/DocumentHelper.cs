using Application.Interfaces.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Application.Helper;


public class DocumentHelper(IDataBase db) 
{
    public async Task EditDocument(Guid workspaceId, Guid documentId, List<DocumentBlock> blocks, Guid creatorId) 
    {
        var existinbBlocks = await db.DocumentBlocks.Where(b => b.DocumentId == documentId).ToListAsync();
        foreach (var block in blocks) 
        {
            if (existinbBlocks.Exists(b => b.Id == block.Id))
            {
                var existingBlock = existinbBlocks.First(b => b.Id == block.Id);
                if (block.Content != existingBlock.Content || block.OrderKey != existingBlock.OrderKey || block.Type != existingBlock.Type)
                {
                    existingBlock.UpdateContent(block.Content);
                    existingBlock.UpdateOrderKey(block.OrderKey);
                    existingBlock.UpdateType(block.Type);
                }
            }
            else 
            {
                var newBlock = DocumentBlock.Create(workspaceId, documentId, block.Type, block.Content, block.OrderKey,creatorId);
                db.DocumentBlocks.Add(newBlock);
            }
        }
        await db.SaveChangesAsync();
    }
}