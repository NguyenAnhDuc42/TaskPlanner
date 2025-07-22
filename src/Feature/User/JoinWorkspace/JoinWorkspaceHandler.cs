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
    private readonly PlannerDbContext _context;
    private readonly IHierarchyRepository _hierarchyRepository;
    private readonly ICurrentUserService _currentUserService;
    public JoinWorkspaceHandler(PlannerDbContext context,IHierarchyRepository hierarchyRepository, ICurrentUserService currentUserService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
    }
    public async Task<Result<JoinWorkspaceRespose, ErrorResponse>> Handle(JoinWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var workspace = await _hierarchyRepository.GetWorkspaceByJoinCodeAsync(request.joinCode, cancellationToken);
        if (workspace == null)
        {
            return Result<JoinWorkspaceRespose, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found."));
        }
        var userId = _currentUserService.CurrentUserId();
        workspace.AddMember(userId, Role.Guest);
        _context.Workspaces.Update(workspace);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<JoinWorkspaceRespose, ErrorResponse>.Success(new JoinWorkspaceRespose(workspace.Id, "You have successfully joined the workspace."));
        
    }
}
