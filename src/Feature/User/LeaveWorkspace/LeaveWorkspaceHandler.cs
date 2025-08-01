using System;
using MediatR;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;

namespace src.Feature.User.LeaveWorkspace;

public class LeaveWorkspaceHandler : IRequestHandler<LeaveWorkspaceRequest, Result<string, ErrorResponse>>
{
    private readonly IUnitOfWork  _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    public LeaveWorkspaceHandler(IUnitOfWork unitOfWork,ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }
    public async Task<Result<string, ErrorResponse>> Handle(LeaveWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var workspace = await _unitOfWork.Workspaces.GetByIdAsync(request.workspaceId, cancellationToken);
        if (workspace == null)
        {
            return Result<string, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found."));
        }
        var userId = _currentUserService.CurrentUserId();
        workspace.RemoveMember(userId);
        _unitOfWork.Workspaces.Update(workspace);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<string, ErrorResponse>.Success("You have successfully left the workspace.");
    }
}
