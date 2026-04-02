using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums.RelationShip;
using MediatR;
using server.Application.Interfaces;
using Application.Helpers;

namespace Application.Features.WorkspaceFeatures.MemberManage.RemoveMembers;

public class RemoveMembersHandler : BaseFeatureHandler, IRequestHandler<RemoveMembersCommand, Guid>
{
    public RemoveMembersHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Guid> Handle(RemoveMembersCommand request, CancellationToken cancellationToken)
    {
        // Direct Find for cleaner resolution
        var workspace = await UnitOfWork.Set<ProjectWorkspace>().FindAsync(request.workspaceId, cancellationToken);
        if (workspace == null) throw new KeyNotFoundException($"Workspace {request.workspaceId} not found");

        if (request.memberIds.Any())
        {
            var sql = @"
                UPDATE workspace_members 
                SET deleted_at = NOW(), 
                    updated_at = NOW() 
                WHERE project_workspace_id = @WorkspaceId 
                  AND user_id = ANY(@UserIds)
                  AND deleted_at IS NULL";

            await UnitOfWork.ExecuteAsync(sql, new
            {
                WorkspaceId = workspace.Id,
                UserIds = request.memberIds.ToArray()
            }, cancellationToken);

        }

        return workspace.Id;
    }
}
