using System;
using MediatR;
using src.Domain.Entities.WorkspaceEntity;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;

namespace src.Feature.WorkspaceManager.CreateSpace;

public class CreateSpaceHandler : IRequestHandler<CreateSpaceRequest, Result<string, ErrorResponse>>
{
    public readonly IUnitOfWork _unitOfWork;
    public readonly ICurrentUserService _currentUserService;


    public CreateSpaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<string, ErrorResponse>> Handle(CreateSpaceRequest request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty)
        {
            return Result<string, ErrorResponse>.Failure(ErrorResponse.Unauthorized());
        }
        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.workspaceId);
        if (workspace == null)
        {
            return Result<string, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found"));
        }
        
        workspace.CreateAndAddSpace(request.body.name, request.body.icon, request.body.color, currentUserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<string, ErrorResponse>.Success("Successfully create space");
    }
}
