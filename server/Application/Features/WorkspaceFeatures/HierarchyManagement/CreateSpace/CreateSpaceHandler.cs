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

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.CreateSpace;

public class CreateSpaceHandler : BaseCommandHandler, IRequestHandler<CreateSpaceCommand, Unit>
{
    public CreateSpaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPermissionService permissionService, WorkspaceContext workspaceContext)
       : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }
    public async Task<Unit> Handle(CreateSpaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Set<ProjectWorkspace>().FindAsync(request.workspaceId, cancellationToken) ?? throw new KeyNotFoundException("Workspace not found");
        await RequirePermissionAsync(workspace, EntityType.ProjectSpace, PermissionAction.Create, cancellationToken);
        var customization = Customization.Create(request.color, request.icon);
        var orderKey = workspace.GetNextOrderAndIncrement();
        if (request.isPrivate)
        {
            var member = EntityMember.AddMember(CurrentUserId, workspace.Id, EntityLayerType.ProjectSpace, AccessLevel.Manager, CurrentUserId);
            await UnitOfWork.Set<EntityMember>().AddAsync(member, cancellationToken);
        }
        var space = ProjectSpace.Create(
            workspaceId: workspace.Id,
            name: request.name,
            description: request.description,
            customization: customization,
            isPrivate: request.isPrivate,
            creatorId: CurrentUserId,
            orderKey: orderKey
        );

        await UnitOfWork.Set<ProjectSpace>().AddAsync(space, cancellationToken);
        return Unit.Value;

    }
}
