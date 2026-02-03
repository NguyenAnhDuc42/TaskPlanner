using Application.Contract.WorkspaceContract;
using Application.Helper;
using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.GetDetailWorkspace;

public class GetDetailWorkspaceHandler : BaseQueryHandler, IRequestHandler<GetDetailWorkspaceQuery, WorkspaceSecurityContextDto>
{
    public GetDetailWorkspaceHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,
        CursorHelper cursorHelper)
        : base(unitOfWork, currentUserService, workspaceContext, cursorHelper)
    {
    }

    public async Task<WorkspaceSecurityContextDto> Handle(GetDetailWorkspaceQuery request, CancellationToken cancellationToken)
    {
        // Simplified query - only fetch security-relevant data
        const string sql = @"
            SELECT 
                w.creator_id as CreatorId,
                wm.role as UserRole
            FROM project_workspaces w
            LEFT JOIN workspace_members wm ON wm.project_workspace_id = w.id 
                AND wm.user_id = @userId AND wm.deleted_at IS NULL AND wm.status = 'Active'
            WHERE w.id = @workspaceId AND w.deleted_at IS NULL";

        var workspaceData = await UnitOfWork.QuerySingleOrDefaultAsync<dynamic>(sql, new { 
            userId = CurrentUserId, 
            workspaceId = request.WorkspaceId 
        }, cancellationToken);

        if (workspaceData == null)
            throw new KeyNotFoundException($"Workspace {request.WorkspaceId} not found");

        // Security check: user must be a member or the creator
        if (workspaceData.UserRole == null && workspaceData.CreatorId != CurrentUserId)
        {
            throw new UnauthorizedAccessException("You don't have access to this workspace");
        }

        // Fetch all permissions in one go (Legacy: Removed for re-implementation)
        var permissions = new List<string>();

        // Determine role
        var role = workspaceData.UserRole != null 
            ? workspaceData.UserRole.ToString() 
            : (workspaceData.CreatorId == CurrentUserId ? Role.Owner.ToString() : Role.Guest.ToString());

        return new WorkspaceSecurityContextDto
        {
            WorkspaceId = request.WorkspaceId,
            CurrentRole = role,
            Permissions = permissions,
            IsOwned = workspaceData.CreatorId == CurrentUserId
        };
    }
}
