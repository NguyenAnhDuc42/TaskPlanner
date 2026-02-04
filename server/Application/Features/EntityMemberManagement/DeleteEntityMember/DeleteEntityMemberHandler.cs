using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.EntityMemberManagement.DeleteEntityMember;

public class DeleteEntityMemberHandler : BaseFeatureHandler, IRequestHandler<DeleteEntityMemberCommand, Unit>
{
    public DeleteEntityMemberHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteEntityMemberCommand request, CancellationToken cancellationToken)
    {
        // Get parent layer
        var layer = await GetLayer(request.LayerId, request.LayerType);

        // Resolve workspace member IDs
        var workspaceMemberIds = await GetWorkspaceMemberIds(request.UserIds, cancellationToken);

        // Find and remove access records
        var accessToRemove = await UnitOfWork.Set<EntityAccess>()
            .Where(ea => ea.EntityId == request.LayerId
                      && ea.EntityLayer == request.LayerType
                      && workspaceMemberIds.Contains(ea.WorkspaceMemberId))
            .ToListAsync(cancellationToken);

        if (accessToRemove.Any())
        {
            UnitOfWork.Set<EntityAccess>().RemoveRange(accessToRemove);
        }

        return Unit.Value;
    }
}
