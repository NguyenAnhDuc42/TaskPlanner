using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Common;
using Microsoft.Extensions.Caching.Hybrid;
using Application.Features;
using Application.Interfaces;

namespace Application.Features.WorkspaceFeatures;

public class CreateWorkspaceHandler(
    IDataBase db, 
    ICurrentUserService currentUserService, 
    HybridCache cache, 
    IRealtimeService realtime
) : ICommandHandler<CreateWorkspaceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateWorkspaceCommand request, CancellationToken ct)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result<Guid>.Failure(UserError.NotFound);

        var workspace = ProjectWorkspace.Create(
            name: request.Name,
            slug: SlugHelper.GenerateSlug(request.Name),
            description: request.Description,
            joinCode: null,
            customization: Customization.Create(request.Color, request.Icon),
            creatorId: currentUserId,
            theme: request.Theme,
            strictJoin: request.StrictJoin
        );
        await db.Workspaces.AddAsync(workspace, ct);

        var workflow = Workflow.Create(workspace.Id, "Default Workflow", null, currentUserId);
        await db.Workflows.AddAsync(workflow, ct);
        var statuses = Status.CreateStarterSet(workspace.Id, workflow.Id, currentUserId);
        await db.Statuses.AddRangeAsync(statuses, ct);

        var space = ProjectSpace.CreateDefault(workspace.Id, currentUserId);
        await db.Spaces.AddAsync(space, ct);
        db.ViewDefinitions.AddRange(
            ViewDefinition.CreateDefaults(workspace.Id, space.Id, EntityLayerType.ProjectSpace, currentUserId));

        var folder = ProjectFolder.CreateDefault(workspace.Id, space.Id, currentUserId);
        await db.Folders.AddAsync(folder, ct);
        db.ViewDefinitions.AddRange(
            ViewDefinition.CreateDefaults(workspace.Id, folder.Id, EntityLayerType.ProjectFolder, currentUserId));

        var firstStatus = statuses.First(s => s.Category == StatusCategory.NotStarted);
        var tasks = ProjectTask.CreateDefaults(workspace.Id, space.Id, folder.Id, firstStatus.Id, currentUserId);
        await db.Tasks.AddRangeAsync(tasks, ct);

        await db.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), ct);
        await realtime.NotifyUserAsync(currentUserId, "WorkspaceCreated", new { WorkspaceId = workspace.Id }, ct);

        return Result<Guid>.Success(workspace.Id);
    }
}
