using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.Relationship;
using Domain.Entities.ProjectEntities;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.EntityAccessManagement.UpdateEntityAccess;

public class UpdateEntityAccessHandler : BaseFeatureHandler, IRequestHandler<UpdateEntityAccessCommand, Unit>
{
    public UpdateEntityAccessHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateEntityAccessCommand request, CancellationToken cancellationToken)
    {
        // Validate layer exists
        var layerExists = request.LayerType switch
        {
            EntityLayerType.ProjectWorkspace => await UnitOfWork.Set<ProjectWorkspace>().AnyAsync(x => x.Id == request.LayerId, cancellationToken),
            EntityLayerType.ProjectSpace => await UnitOfWork.Set<ProjectSpace>().AnyAsync(x => x.Id == request.LayerId, cancellationToken),
            EntityLayerType.ProjectFolder => await UnitOfWork.Set<ProjectFolder>().AnyAsync(x => x.Id == request.LayerId, cancellationToken),
            EntityLayerType.ChatRoom => await UnitOfWork.Set<ChatRoom>().AnyAsync(x => x.Id == request.LayerId, cancellationToken),
            _ => false
        };

        if (!layerExists) throw new KeyNotFoundException($"{request.LayerType} {request.LayerId} not found");

        // Resolve workspace member IDs
        var workspaceMemberIds = await GetWorkspaceMemberIds(request.UserIds, cancellationToken);

        // Find and update access records
        var accessToUpdate = await UnitOfWork.Set<EntityAccess>()
            .Where(ea => ea.ProjectWorkspaceId == WorkspaceId
                      && ea.EntityId == request.LayerId
                      && ea.EntityLayer == request.LayerType
                      && workspaceMemberIds.Contains(ea.WorkspaceMemberId))
            .ToListAsync(cancellationToken);

        if (accessToUpdate.Any())
        {
            accessToUpdate.ForEach(ea => ea.UpdateAccessLevel(request.AccessLevel));
        }

        return Unit.Value;
    }
}
