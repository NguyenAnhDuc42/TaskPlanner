

using MediatR;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;

namespace src.Feature.SpaceManager.CreateSpace;

public class CreateSpaceHandler : IRequestHandler<CreateSpaceRequest, Guid>
{
    public readonly IUnitOfWork _unitOfWork;
    public readonly ICurrentUserService _currentUserService;


    public CreateSpaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateSpaceRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }
        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.workspaceId);
        if (workspace == null)
        {
            throw new KeyNotFoundException("Workspace not found");
        }
        
        var newSpace = workspace.CreateAndAddSpace(request.body.name, request.body.icon, request.body.color, currentUserId);
        await _unitOfWork.Spaces.AddAsync(newSpace, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return newSpace.Id;
    }
}
