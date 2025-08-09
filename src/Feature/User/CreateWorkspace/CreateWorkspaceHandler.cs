using System;
using MediatR;
using src.Domain.Entities.WorkspaceEntity;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;

namespace src.Feature.User.CreateWorkspace;

public class CreateWorkspaceHandler : IRequestHandler<CreateWorkspaceRequest, Result<string, ErrorResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CreateWorkspaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<string, ErrorResponse>> Handle(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUserId();
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return Result<string, ErrorResponse>.Failure(
                ErrorResponse.Unauthorized("User not found"));
        }
        var workspace = Workspace.Create(
            request.Name,
            request.Description,
            request.Color,
            request.Icon,
            userId,
            request.IsPrivate);

        user.CreateWorkspace(workspace);
        await _unitOfWork.Workspaces.AddAsync(workspace);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (result <= 0)
        {
            return Result<string, ErrorResponse>.Failure(
                ErrorResponse.Internal("Failed to save workspace"));
        }

        return Result<string, ErrorResponse>.Success(workspace.Id.ToString());
    }
}
