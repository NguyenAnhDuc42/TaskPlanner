using Microsoft.EntityFrameworkCore;
namespace Application;

public class CreateSpaceHandler(
    TaskPlanDbContext db, 
    WorkspaceContext context,
    RealtimeService realtime
) : ICommandHandler<CreateSpaceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateSpaceCommand request, CancellationToken ct)
    {
        return await db.ExecuteInTransactionAsync(async () =>
        {
            var maxKey = await db.ProjectSpaces
                .AsNoTracking()
                .ByWorkspace(context.workspaceId)
                .WhereNotDeleted()
                .MaxAsync(s => s.OrderKey, ct);
            
            var orderKey = maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
            var slug = SlugHelper.GenerateSlug(request.name);

            // 1. Create the primary document for this space
            var document = Document.Create(
                context.workspaceId,
                request.name, 
                context.CurrentMember.Id
            );
            await db.Documents.AddAsync(document, ct);

            // 2. Create the space linked to the document
            var space = ProjectSpace.Create(
                projectWorkspaceId: context.workspaceId,
                name: request.name,
                slug: slug,
                defaultDocumentId: document.Id,
                color: request.color,
                icon: request.icon,
                isPrivate: request.isPrivate,
                creatorId: context.CurrentMember.Id,
                orderKey: orderKey
            );

            await db.ProjectSpaces.AddAsync(space, ct);

            // 3. Create Default Workflow for the space
            var workflow = Workflow.Create(
                context.workspaceId, 
                $"{request.name} Workflow", 
                $"Default workflow for {request.name} space", 
                context.CurrentMember.Id, 
                projectSpaceId: space.Id
            );
            await db.Workflows.AddAsync(workflow, ct);

            // 4. Create Starter Statuses
            var statuses = Status.CreateSpaceStarterSet(context.workspaceId, workflow.Id, context.CurrentMember.Id);
            await db.Statuses.AddRangeAsync(statuses, ct);

            // 5. Create Default Views
            db.ViewDefinitions.AddRange(
                ViewDefinition.CreateDefaults(context.workspaceId, space.Id, null, context.CurrentMember.Id));

            await realtime.NotifyWorkspaceAsync(context.workspaceId, "SpaceCreated", new { SpaceId = space.Id, WorkspaceId = context.workspaceId }, ct);

            return Result<Guid>.Success(space.Id);
        }, ct);
    }
}



