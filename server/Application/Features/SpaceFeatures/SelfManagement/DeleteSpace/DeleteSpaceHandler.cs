using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.SpaceFeatures.SelfManagement.DeleteSpace;

public class DeleteSpaceHandler : BaseCommandHandler, IRequestHandler<DeleteSpaceCommand, Unit>
{
    public DeleteSpaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPermissionService permissionService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteSpaceCommand request, CancellationToken cancellationToken)
    {
        var space = await UnitOfWork.Set<ProjectSpace>().FindAsync(request.spaceId, cancellationToken) ?? throw new KeyNotFoundException("Space not found");
        await RequirePermissionAsync(space, PermissionAction.Delete, cancellationToken);
        UnitOfWork.Set<ProjectSpace>().Remove(space);
        return Unit.Value;
    }
}
