using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Domain;

namespace Application;

public class CreateSpaceDocumentHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext) : ICommandHandler<CreateSpaceDocumentCommand, SpaceDocumentRecord>
{
    public async Task<Result<SpaceDocumentRecord>> Handle(CreateSpaceDocumentCommand request, CancellationToken ct)
    {
        // 1. Verify Space exists in Workspace
        var spaceExists = await db.ProjectSpaces
            .AnyAsync(s => s.Id == request.SpaceId && s.ProjectWorkspaceId == workspaceContext.workspaceId && s.DeletedAt == null, ct);

        if (!spaceExists)
            return Result<SpaceDocumentRecord>.Failure(Error.NotFound("Space.NotFound", $"Space {request.SpaceId} not found"));

        // 2. Create the Document entity
        var document = Document.Create(workspaceContext.workspaceId, request.Name, workspaceContext.CurrentMember.Id);
        await db.Documents.AddAsync(document, ct);

        // 3. Link Document to Space using EntityAssetLink with AssetType.Document
        var link = EntityAssetLink.Create(
            workspaceContext.workspaceId,
            document.Id,
            AssetType.Document,
            projectSpaceId: request.SpaceId,
            projectFolderId: null,
            projectTaskId: null,
            commentId: null,
            creatorId: workspaceContext.CurrentMember.Id
        );
        await db.EntityAssetLinks.AddAsync(link, ct);

        // 4. Save changes
        await db.SaveChangesAsync(ct);

        // 5. Return mapped record
        var record = new SpaceDocumentRecord(document.Id, document.Name, IsDefault: false);
        return Result<SpaceDocumentRecord>.Success(record);
    }
}
