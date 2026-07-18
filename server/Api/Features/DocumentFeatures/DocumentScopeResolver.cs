using Microsoft.EntityFrameworkCore;

namespace Api;

// DocumentBlock has no direct space FK — a document is reachable via a Space's
// DefaultDocumentId, a Documents tree row, a Task's DefaultDocumentId, or (for freestanding
// attachments) an EntityAssetLink. Mirrors the resolution order used by the legacy
// UpdateDocumentBlocksHandler. No FK is added from DocumentBlock.DocumentId to the Documents
// table — it stays an opaque, unconstrained GUID shared across all four owner kinds.
public static class DocumentScopeResolver
{
    public static async Task<Guid> ResolveSpaceIdAsync(TaskPlanDbContext db, Guid documentId, CancellationToken cancellationToken)
    {
        return await db.ProjectSpaces
                .Where(s => s.DefaultDocumentId == documentId)
                .Select(s => (Guid?)s.Id)
                .FirstOrDefaultAsync(cancellationToken)
            ?? await db.Documents
                .Where(d => d.Id == documentId && d.DeletedAt == null)
                .Select(d => (Guid?)d.ProjectSpaceId)
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
