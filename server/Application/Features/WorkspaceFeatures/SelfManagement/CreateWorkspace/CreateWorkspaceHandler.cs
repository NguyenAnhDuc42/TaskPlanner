using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities.ProjectEntities;
using Domain.Entities.ProjectEntities.ValueObject;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.CreateWorkspace;

public class CreateWorkspaceHandler : ICommandHandler<CreateWorkspaceCommand, Guid>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;

    public CreateWorkspaceHandler(IDataBase db, ICurrentUserService currentUserService, HybridCache cache, IRealtimeService realtime)
    {
        _db = db;
        _currentUserService = currentUserService;
        _cache = cache;
        _realtime = realtime;
    }

    public async Task<Result<Guid>> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result.Failure<Guid>(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

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

        await _db.Workspaces.AddAsync(workspace, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        
        await _cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), cancellationToken);

        _ = _realtime.NotifyUserAsync(currentUserId, "WorkspaceCreated", new { WorkspaceId = workspace.Id }, default);

        return Result.Success(workspace.Id);
    }
}
