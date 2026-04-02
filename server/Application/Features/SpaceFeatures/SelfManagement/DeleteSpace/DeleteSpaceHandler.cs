using System;
using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.SpaceFeatures.SelfManagement.DeleteSpace;

public class DeleteSpaceHandler : BaseFeatureHandler, IRequestHandler<DeleteSpaceCommand, Unit>
{
    public DeleteSpaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteSpaceCommand request, CancellationToken cancellationToken)
    {
        var space = await UnitOfWork.Set<ProjectSpace>().FindAsync(request.SpaceId, cancellationToken);
        if (space == null) throw new KeyNotFoundException($"Space {request.SpaceId} not found");
        space.SoftDelete();
        return Unit.Value;
    }
}
