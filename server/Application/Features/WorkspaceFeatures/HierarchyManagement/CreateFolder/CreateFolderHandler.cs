using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.CreateFolder;

public class CreateFolderHandler : BaseCommandHandler, IRequestHandler<CreateFolderCommand, Unit>
{
    public CreateFolderHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPermissionService permissionService, WorkspaceContext workspaceContext)
       : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
        var space = await UnitOfWork.Set<ProjectSpace>().FindAsync(request.spaceId, cancellationToken) ?? throw new KeyNotFoundException("Space not found");
        
        await RequirePermissionAsync(space, EntityType.ProjectFolder, PermissionAction.Create, cancellationToken);
        var customization = Customization.Create(request.color, request.icon);
        var orderKey = space.GetNextListOrderAndIncrement();
        if (request.isPrivate)
        {
            var member = EntityMember.AddMember(CurrentUserId, space.Id, EntityLayerType.ProjectFolder, AccessLevel.Manager, CurrentUserId);
            await UnitOfWork.Set<EntityMember>().AddAsync(member, cancellationToken);
        }
        var folder = ProjectFolder.Create(
            projectSpaceId: space.Id,
            name: request.name,
            color: customization.Color,
            icon: customization.Icon,
            isPrivate: request.isPrivate, // This was ownerId before, but ProjectFolder.Create expects creatorId
            creatorId: CurrentUserId,
            orderKey: orderKey
        );

        await UnitOfWork.Set<ProjectFolder>().AddAsync(folder, cancellationToken);
        return Unit.Value;
    }
}
