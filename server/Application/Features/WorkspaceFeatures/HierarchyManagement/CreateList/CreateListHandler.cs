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

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.CreateList;

public class CreateListHandler : BaseCommandHandler, IRequestHandler<CreateListCommand, Unit>
{
    public CreateListHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPermissionService permissionService, WorkspaceContext workspaceContext)
       : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(CreateListCommand request, CancellationToken cancellationToken)
    {
        var space = await UnitOfWork.Set<ProjectSpace>().FindAsync(request.spaceId, cancellationToken) ?? throw new KeyNotFoundException("Space not found");

        ProjectFolder? folder = null;
        if (request.folderId.HasValue)
        {
            folder = await UnitOfWork.Set<ProjectFolder>()
                .Where(f => f.Id == request.folderId.Value)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException($"Folder {request.folderId} not found");

            if (folder.ProjectSpaceId != space.Id)
            {
                throw new ValidationException("Folder does not belong to space");
            }
        }
        await RequirePermissionAsync(space, EntityType.ProjectList, PermissionAction.Create, cancellationToken);

        long orderKey = folder?.GetNextListOrderAndIncrement() ?? space.GetNextListOrderAndIncrement();
        var customization = Customization.Create(request.color, request.icon);
        var list = ProjectList.Create(
            spaceId: space.Id, folderId: request.folderId,
            name: request.name, customization: customization,
            isPrivate: request.isPrivate,
            creatorId: CurrentUserId,
            startDate: request.startDate,
            dueDate: request.dueDate,
            orderKey: orderKey);

        await UnitOfWork.Set<ProjectList>().AddAsync(list, cancellationToken);

        if (request.isPrivate)
        {
            var member = EntityMember.AddMember(
                userId: CurrentUserId,
                entityId: list.Id,
                entityLayer: EntityLayerType.ProjectList,
                accessLevel: AccessLevel.Manager,
                addedBy: CurrentUserId
            );

            await UnitOfWork.Set<EntityMember>().AddAsync(member, cancellationToken);
        }
        return Unit.Value;
    }
}
