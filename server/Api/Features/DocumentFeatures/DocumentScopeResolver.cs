using Microsoft.EntityFrameworkCore;

namespace Api;

// Document/DocumentBlock have no direct space FK — a document is reachable via a Space's
// DefaultDocumentId, a Task's DefaultDocumentId, or (for freestanding attachments) an
// EntityAssetLink. Mirrors the resolution order used by the legacy UpdateDocumentBlocksHandler.
public static class DocumentScopeResolver
{
    public static async Task<Guid> ResolveSpaceIdAsync(TaskPlanDbContext db, Guid documentId, CancellationToken cancellationToken)
    {
        return await db.ProjectSpaces
                .Where(s => s.DefaultDocumentId == documentId)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(cancellationToken)
            ?? await db.ProjectTasks
                .Where(t => t.DefaultDocumentId == documentId)
                .Select(t => (Guid?)t.ProjectSpaceId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? await db.EntityAssetLinks
                .Where(l => l.AssetId == documentId && l.AssetType == AssetType.Document && l.ProjectSpaceId != null)
                .Select(l => l.ProjectSpaceId)
                .FirstOrDefaultAsync(cancellationToken)
            ?? Guid.Empty;
    }
}
