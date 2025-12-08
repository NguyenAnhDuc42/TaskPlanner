using Application.Contract.WorkspaceContract;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.ProjectEntities.ValueObject;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.CreateWorkspace;

public class CreateWorkspaceHandler : IRequestHandler<CreateWorkspaceCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateWorkspaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
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

        return workspace.Id;
    }
}