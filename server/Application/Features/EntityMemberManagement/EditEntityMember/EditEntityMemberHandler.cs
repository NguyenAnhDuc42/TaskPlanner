using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.EntityMemberManagement.EditEntityMember;

public class EditEntityMemberHandler : BaseFeatureHandler, IRequestHandler<EditEntityMemberCommand, Unit>
{
    public EditEntityMemberHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(EditEntityMemberCommand request, CancellationToken cancellationToken)
    {
        // Get parent layer
        var layer = await GetLayer(request.LayerId, request.LayerType);

        // Find and update members
        var membersToUpdate = await UnitOfWork.Set<EntityMember>()
            .Where(em => em.LayerId == request.LayerId
                      && em.LayerType == request.LayerType
                      && request.UserIds.Contains(em.UserId))
            .ToListAsync(cancellationToken);

        if (membersToUpdate.Any())
        {
            membersToUpdate.ForEach(em => em.UpdateAccessLevel(request.AccessLevel));
        }

        return Unit.Value;
    }
}
