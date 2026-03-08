using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.TaskFeatures.AssigneeManagement.GetTaskListAssignees;

public class GetTaskListAssigneesHandler : BaseFeatureHandler, IRequestHandler<GetTaskListAssigneesQuery, List<TaskAssigneeOptionDto>>
{
    public GetTaskListAssigneesHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<List<TaskAssigneeOptionDto>> Handle( GetTaskListAssigneesQuery request, CancellationToken cancellationToken)
    {
        await GetLayer(request.ListId, EntityLayerType.ProjectList);

        var currentWorkspaceMemberId = await WorkspaceContext.GetWorkspaceMemberIdAsync(cancellationToken);
        var currentMemberHasAccess = await GetAccessibleMemberIds(
            request.ListId,
            EntityLayerType.ProjectList,
            new List<Guid> { currentWorkspaceMemberId });

        if (currentMemberHasAccess.Count == 0) return new List<TaskAssigneeOptionDto>();

        var allWorkspaceMemberIds = await UnitOfWork.Set<WorkspaceMember>()
            .AsNoTracking()
            .Where(wm => wm.ProjectWorkspaceId == WorkspaceId && wm.DeletedAt == null)
            .Select(wm => wm.Id)
            .ToListAsync(cancellationToken);

        var accessibleMemberIds = await GetAccessibleMemberIds(
            request.ListId,
            EntityLayerType.ProjectList,
            allWorkspaceMemberIds);

        if (accessibleMemberIds.Count == 0) return new List<TaskAssigneeOptionDto>();

        var members = await UnitOfWork.QueryAsync<TaskAssigneeOptionDto>(@"
            SELECT u.id AS UserId, u.name AS UserName, NULL AS AvatarUrl
            FROM workspace_members wm
            JOIN users u ON wm.user_id = u.id
            WHERE wm.id = ANY(@WorkspaceMemberIds)
              AND wm.deleted_at IS NULL
            ORDER BY u.name", new { WorkspaceMemberIds = accessibleMemberIds.ToArray() }, cancellationToken);

        return members.ToList();
    }
}
