using Microsoft.EntityFrameworkCore;

namespace Application;

public class CreateSpaceDocumentHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService
) : ICommandHandler<CreateSpaceDocumentCommand, SpaceDocumentRecord>
{
    public async Task<Result<SpaceDocumentRecord>> Handle(CreateSpaceDocumentCommand request, CancellationToken cancellationToken)
    {
        var space = await db.ProjectSpaces
            .AsNoTracking()
            .Where(s => s.Id == request.SpaceId
                     && s.DeletedAt == null)
            .Select(s => new { s.CreatorId })
            .FirstOrDefaultAsync(cancellationToken);

        if (space is null) return Result<SpaceDocumentRecord>.Failure(SpaceError.NotFound);

        var hasAccess = await permissionService.VerifyAsync(Role.Member, request.SpaceId, AccessLevel.Editor, space.CreatorId, cancellationToken);
        if (!hasAccess) return Result<SpaceDocumentRecord>.Failure(MemberError.DontHavePermission);

        var document = Document.Create(workspaceContext.WorkspaceId, request.Name, workspaceContext.CurrentMember.Id);
        db.Documents.Add(document);

        var link = EntityAssetLink.Create(
            workspaceContext.WorkspaceId,
            document.Id,
            AssetType.Document,
            projectSpaceId: request.SpaceId,
            projectFolderId: null,
            projectTaskId: null,
            commentId: null,
            creatorId: workspaceContext.CurrentMember.Id
        );
        db.EntityAssetLinks.Add(link);

        await db.SaveChangesAsync(cancellationToken);

        return Result<SpaceDocumentRecord>.Success(
            new SpaceDocumentRecord(document.Id, document.Name, IsDefault: false)
        );
    }
}