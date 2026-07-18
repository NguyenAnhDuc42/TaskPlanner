using Microsoft.EntityFrameworkCore;

namespace Api;

// Shared by DeleteDocumentHandler (online path) and BatchFlushHandler's offline Document delete
// case — a Document delete recursively removes its whole subtree (unlike Folder, which reparents
// orphaned Tasks instead of cascading; deleting a wiki page should remove its sub-pages).
public static class DocumentCascadeHelper
{
    // Recursive CTE via EF's FromSqlInterpolated — runs on the same DbContext/connection, so it
    // participates in the caller's ambient transaction automatically. rootId is included in the
    // returned set.
    public static async Task<List<Guid>> GetDescendantIdsAsync(TaskPlanDbContext db, Guid rootId, CancellationToken cancellationToken)
    {
        return await db.Documents
            .FromSqlInterpolated($@"
                WITH RECURSIVE descendants AS (
                    SELECT * FROM documents WHERE id = {rootId} AND deleted_at IS NULL
                    UNION ALL
                    SELECT d.* FROM documents d
                    INNER JOIN descendants ds ON d.parent_document_id = ds.id
                    WHERE d.deleted_at IS NULL
                )
                SELECT * FROM descendants")
            .AsNoTracking()
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);
    }

    public static async Task CascadeDeleteAsync(TaskPlanDbContext db, List<Guid> documentIds, CancellationToken cancellationToken)
    {
        if (documentIds.Count == 0) return;
        var now = DateTimeOffset.UtcNow;

        await db.Documents
            .Where(d => documentIds.Contains(d.Id) && d.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.DeletedAt, now)
                .SetProperty(d => d.UpdatedAt, now), cancellationToken);

        await db.DocumentBlocks
            .Where(b => documentIds.Contains(b.DocumentId) && b.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.DeletedAt, now)
                .SetProperty(b => b.UpdatedAt, now), cancellationToken);
    }
}
