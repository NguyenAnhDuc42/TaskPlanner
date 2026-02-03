using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.DeleteWorkspace;

public class DeleteWorkspaceHandler : BaseFeatureHandler, IRequestHandler<DeleteWorkspaceCommand, Unit>
{
    public DeleteWorkspaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await FindOrThrowAsync<ProjectWorkspace>(request.workspaceId);
        workspace.SoftDelete();

        return Unit.Value;

    }
}
