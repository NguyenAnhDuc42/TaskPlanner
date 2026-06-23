using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Application;

public class CreateSpaceHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    PermissionService permissionService,
    RealtimeService realtimeService,
    ILogger<CreateSpaceHandler> logger
) : ICommandHandler<CreateSpaceCommand, SpaceRecord>
{
    public async Task<Result<SpaceRecord>> Handle(CreateSpaceCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to create space {SpaceName} by member {MemberId}", request.name, context.CurrentMember.Id);
        var hasAccess = await permissionService.VerifyAsync(Role.Member, cancellationToken: cancellationToken);
        if (!hasAccess) return Result<SpaceRecord>.Failure(MemberError.DontHavePermission);

        ProjectSpace? space = null;
        EntityAccess? creatorAccess = null;
        List<Status> createdStatuses = new();

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var maxKey = await db.ProjectSpaces
                .AsNoTracking()
                .Where(s => s.ProjectWorkspaceId == context.WorkspaceId && s.DeletedAt == null)
                .MaxAsync(s => s.OrderKey, cancellationToken);

            var orderKey = FractionalIndex.SafeAfter(maxKey);
            var slug = SlugHelper.GenerateSlug(request.name);

            var document = Document.Create(
                context.WorkspaceId,
                request.name,
                context.CurrentMember.Id
            );
            await db.Documents.AddAsync(document, cancellationToken);

            space = ProjectSpace.Create(
                projectWorkspaceId: context.WorkspaceId,
                name: request.name,
                slug: slug,
                defaultDocumentId: document.Id,
                color: request.color,
                icon: request.icon,
                isPrivate: request.isPrivate,
                creatorId: context.CurrentMember.Id,
                orderKey: orderKey
            );
            await db.ProjectSpaces.AddAsync(space, cancellationToken);

            var statuses = Status.CreateSpaceStarterSet(context.WorkspaceId, space.Id, context.CurrentMember.Id);
            await db.Statuses.AddRangeAsync(statuses, cancellationToken);
            createdStatuses.AddRange(statuses);

            creatorAccess = EntityAccess.Create(
                projectWorkspaceId: context.WorkspaceId,
                workspaceMemberId: context.CurrentMember.Id,
                projectSpaceId: space.Id,
                projectFolderId: null,
                projectTaskId: null,
                accessLevel: AccessLevel.Manager,
                creatorId: context.CurrentMember.Id
            );
            await db.EntityAccesses.AddAsync(creatorAccess, cancellationToken);

            return Result<SpaceRecord>.Success(SpaceRecord.FromDomain(space!));
        }, cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation("Broadcasting entity updates for created space {SpaceId}", result.Value?.Id);
            _ = realtimeService
            .NotifyEntitiesUpdatedAsync(context.WorkspaceId,
               new EntityBatchUpdate {
                   Spaces = [result.Value!],
                   EntityAccess = [EntityAccessRecord.FromDomain(creatorAccess!)],
                   Statuses = createdStatuses.Select(StatusRecord.FromDomain).ToList()
               }, default)
            .ContinueWith(t =>
                logger.LogError(t.Exception, "Failed to send real-time notification for created space {SpaceId}", result.Value?.Id),
                TaskContinuationOptions.OnlyOnFaulted);
        }
        return result;
    }
}
