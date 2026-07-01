namespace Api;

public class CreateWorkspaceHandler(
    TaskPlanDbContext db,
    CurrentUserService currentUserService,
    WorkspaceService workspaceService,
    ILogger<CreateWorkspaceHandler> logger
) : ICommandHandler<CreateWorkspaceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var creatorUserId = currentUserService.CurrentUserId();
        if (creatorUserId == Guid.Empty)
            return Result<Guid>.Failure(Error.Unauthorized("Auth.Required", "Authenticated user required."));

        logger.LogInformation("Creating workspace '{WorkspaceName}' for user {UserId}", request.Name, creatorUserId);

        var workspace = ProjectWorkspace.Create(
            id: request.Id,
            name: request.Name,
            slug: SlugHelper.GenerateSlug(request.Name),
            description: request.Description ?? string.Empty,
            joinCode: null,
            color: request.Color,
            icon: request.Icon,
            creatorId: creatorUserId,
            theme: request.Theme ?? Theme.Dark,
            strictJoin: request.StrictJoin ?? false
        );

        db.ProjectWorkspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);

        workspaceService.InitializeInBackground(workspace.Id, creatorUserId);

        logger.LogInformation("Created workspace {WorkspaceId}", workspace.Id);
        return Result<Guid>.Success(workspace.Id);
    }
}
