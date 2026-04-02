using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.Relationship;
using Domain.Entities.ProjectEntities;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.EntityAccessManagement.RemoveEntityAccess;

public class RemoveEntityAccessHandler : BaseFeatureHandler, IRequestHandler<RemoveEntityAccessCommand, Unit>
{
    public RemoveEntityAccessHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(RemoveEntityAccessCommand request, CancellationToken cancellationToken)
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

        // Find and remove access records
        var accessToRemove = await UnitOfWork.Set<EntityAccess>()
            .Where(ea => ea.ProjectWorkspaceId == WorkspaceId
                      && ea.EntityId == request.LayerId
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
