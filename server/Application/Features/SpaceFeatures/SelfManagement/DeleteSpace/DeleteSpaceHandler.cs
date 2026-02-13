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
        var space = await FindOrThrowAsync<ProjectSpace>(request.SpaceId);
        space.SoftDelete();
        return Unit.Value;
    }
}
