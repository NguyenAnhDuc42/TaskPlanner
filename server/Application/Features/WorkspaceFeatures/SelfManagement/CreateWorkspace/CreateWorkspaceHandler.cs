using Application.Contract.WorkspaceContract;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.ProjectEntities.ValueObject;
using MediatR;
using server.Application.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application.Features.WorkspaceFeatures.SelfManagement.CreateWorkspace;

public class CreateWorkspaceHandler : IRequestHandler<CreateWorkspaceCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly HybridCache _cache;

    public CreateWorkspaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, HybridCache cache)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _cache = cache;
    }

    public async Task<Guid> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var variant = request.Variant;
        var theme = request.Theme;
        var customization = Customization.Create(request.Color, request.Icon);
        var workspace = ProjectWorkspace.Create(
            name: request.Name,
            description: request.Description,
            joinCode: null, // Let the entity generate it
            customization: customization,
            creatorId: currentUserId,
            theme: theme,
            variant: variant,
            strictJoin: request.StrictJoin
        );

        await _unitOfWork.Set<ProjectWorkspace>().AddAsync(workspace, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        // Invalidate user's workspace list cache
        await _cache.RemoveByTagAsync($"user:{currentUserId}:workspaces", cancellationToken);

        return workspace.Id;
    }
}