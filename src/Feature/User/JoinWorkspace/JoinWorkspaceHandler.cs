using System;
using MediatR;
using src.Domain.Enums;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;
using src.Infrastructure.Data;

namespace src.Feature.User.JoinWorkspace;

public class JoinWorkspaceHandler : IRequestHandler<JoinWorkspaceRequest, Result<JoinWorkspaceRespose, ErrorResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    public JoinWorkspaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }
    public async Task<Result<JoinWorkspaceRespose, ErrorResponse>> Handle(JoinWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var workspace = await _unitOfWork.Workspaces.GetByJoinCodeAsync(request.joinCode, cancellationToken);
        if (workspace == null)
        {
            return Result<JoinWorkspaceRespose, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found."));
        }
        var userId = _currentUserService.CurrentUserId();
        workspace.AddMember(userId, Role.Guest);
        _unitOfWork.Workspaces.Update(workspace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<JoinWorkspaceRespose, ErrorResponse>.Success(new JoinWorkspaceRespose(workspace.Id, "You have successfully joined the workspace."));

    }
}
