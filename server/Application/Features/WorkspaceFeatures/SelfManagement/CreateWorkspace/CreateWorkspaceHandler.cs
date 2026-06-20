using Microsoft.Extensions.Caching.Hybrid;
namespace Application;

public class CreateWorkspaceHandler(
    TaskPlanDbContext db,
    CurrentUserService currentUserService,
    HybridCache cache,
    WorkspaceService workspaceService
) : ICommandHandler<CreateWorkspaceCommand, WorkspaceSnippetRecord>
{
    public async Task<Result<WorkspaceSnippetRecord>> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty)
            return Result<WorkspaceSnippetRecord>.Failure(UserError.NotFound);

        var workspace = ProjectWorkspace.Create(
            name: request.Name,
            slug: SlugHelper.GenerateSlug(request.Name),
            description: request.Description ?? string.Empty,
            joinCode: null,
            color: request.Color,
            icon: request.Icon,
            creatorId: currentUserId,
            theme: request.Theme,
            strictJoin: request.StrictJoin
        );

        await db.ProjectWorkspaces.AddAsync(workspace, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), cancellationToken);

        workspaceService.InitializeInBackground(workspace.Id, currentUserId);

        var snippet = new WorkspaceSnippetRecord
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Icon = workspace.Icon,
            Color = workspace.Color,
            Role = Role.Owner,
            IsPinned = false,
            MemberCount = 1,
        };

        return Result<WorkspaceSnippetRecord>.Success(snippet);
    }
}
