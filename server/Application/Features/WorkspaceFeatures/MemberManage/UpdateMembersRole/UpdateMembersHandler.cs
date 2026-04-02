using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using Application.Helpers;

namespace Application.Features.WorkspaceFeatures.MemberManage.UpdateMembers;

public class UpdateMembersHandler : BaseFeatureHandler, IRequestHandler<UpdateMembersCommand, Unit>
{
    public UpdateMembersHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateMembersCommand request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Set<ProjectWorkspace>().FindAsync(request.workspaceId, cancellationToken);
        if (workspace == null) throw new KeyNotFoundException($"Workspace {request.workspaceId} not found");
        if (request.members == null || !request.members.Any()) return Unit.Value;

        foreach (var memberUpdate in request.members)
        {
            var sql = @"
                UPDATE workspace_members 
                SET role = COALESCE(@Role, role), 
                    membership_status = COALESCE(@Status, membership_status), 
                    updated_at = NOW() 
                WHERE project_workspace_id = @WorkspaceId 
                  AND user_id = @UserId 
                  AND role != 'Owner' -- Protection for owners
                  AND deleted_at IS NULL";

            await UnitOfWork.ExecuteAsync(sql, new
            {
                WorkspaceId = workspace.Id,
                UserId = memberUpdate.userId,
                Role = memberUpdate.role?.ToString(),
                Status = memberUpdate.status?.ToString()
            }, cancellationToken);
        }

        return Unit.Value;
    }
}
