using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.ProjectEntities.ValueObject;
using Application.Features.WorkspaceFeatures.SelfManagement;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Application.Interfaces;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.CreateWorkspace;

public class CreateWorkspaceHandler : BaseFeatureHandler, IRequestHandler<CreateWorkspaceCommand, Guid>
{
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;

    public CreateWorkspaceHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,
        HybridCache cache,
        IRealtimeService realtime)
        : base(unitOfWork, currentUserService, workspaceContext) 
    {
        _cache = cache;
        _realtime = realtime;
    }

    public async Task<Guid> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = ValidateRequest();
        
        var workspace = await PersistWorkspace(request, currentUserId, cancellationToken);
        
        await InvalidateCache(currentUserId, cancellationToken);
        
        NotifyClients(workspace.Id, currentUserId);

        return workspace.Id;
    }

    private Guid ValidateRequest()
    {
        var currentUserId = CurrentUserId;
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }
        return currentUserId;
    }

    private async Task<ProjectWorkspace> PersistWorkspace(CreateWorkspaceCommand request, Guid currentUserId, CancellationToken ct)
    {
        var customization = Customization.Create(request.Color, request.Icon);
        var workspace = ProjectWorkspace.Create(
            name: request.Name,
            description: request.Description,
            joinCode: null,
            customization: customization,
            creatorId: currentUserId,
            theme: request.Theme,
            strictJoin: request.StrictJoin
        );

        await UnitOfWork.Set<ProjectWorkspace>().AddAsync(workspace, ct);
        
        return workspace;
    }

    private async Task InvalidateCache(Guid userId, CancellationToken ct)
    {
        await _cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(userId), ct);
    }

    private void NotifyClients(Guid workspaceId, Guid userId)
    {
        _ = _realtime.NotifyUserAsync(userId, "WorkspaceCreated", new { WorkspaceId = workspaceId }, default);
    }
}
