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
        Workflow? workflow = null;
        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var maxKey = await db.ProjectSpaces
                .AsNoTracking()
                .ByWorkspace(context.WorkspaceId)
                .WhereNotDeleted()
                .MaxAsync(s => s.OrderKey, cancellationToken);

            var orderKey = maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
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

            workflow = Workflow.Create(
                context.WorkspaceId,
                $"{request.name} Workflow",
                $"Default workflow for {request.name} space",
                context.CurrentMember.Id,
                projectSpaceId: space.Id
            );
            await db.Workflows.AddAsync(workflow, cancellationToken);

            var statuses = Status.CreateSpaceStarterSet(context.WorkspaceId, workflow.Id, context.CurrentMember.Id);
            await db.Statuses.AddRangeAsync(statuses, cancellationToken);

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

            var record = SpaceRecord.FromDomain(space!, workflow!.Id);
            return Result<SpaceRecord>.Success(record);
        }, cancellationToken);
        
        if (result.IsSuccess)
        {
            logger.LogInformation("Broadcasting entity updates for created space {SpaceId}", result.Value.Id);
            _ = realtimeService.NotifyEntitiesUpdatedAsync(
                context.TryGetWorkspaceId().Value,
               new EntityBatchUpdate
               {
                   Spaces = [result.Value],
                   EntityAccess = [EntityAccessRecord.FromDomain(creatorAccess!)]
               },
                default
            );
        }
        return result;
    }
}



