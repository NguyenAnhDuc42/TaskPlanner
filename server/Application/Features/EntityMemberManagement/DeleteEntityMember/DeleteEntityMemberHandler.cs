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

        // Find and remove members
        var membersToRemove = await UnitOfWork.Set<EntityMember>()
            .Where(em => em.LayerId == request.LayerId
                      && em.LayerType == request.LayerType
                      && request.UserIds.Contains(em.UserId))
            .ToListAsync(cancellationToken);

        if (membersToRemove.Any())
        {
            UnitOfWork.Set<EntityMember>().RemoveRange(membersToRemove);
        }

        return Unit.Value;
    }
}
