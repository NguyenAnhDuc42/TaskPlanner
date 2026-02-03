using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.TaskFeatures.SelfManagement.DeleteTask;

public class DeleteTaskHandler : BaseFeatureHandler, IRequestHandler<DeleteTaskCommand, Unit>
{
    public DeleteTaskHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await FindOrThrowAsync<ProjectTask>(request.TaskId);

        task.SoftDelete();

        return Unit.Value;
    }
}
