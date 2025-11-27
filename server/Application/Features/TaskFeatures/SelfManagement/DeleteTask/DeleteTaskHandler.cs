using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.TaskFeatures.SelfManagement.DeleteTask;

public class DeleteTaskHandler : BaseCommandHandler, IRequestHandler<DeleteTaskCommand, Unit>
{
    public DeleteTaskHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await FindOrThrowAsync<ProjectTask>(request.TaskId) as ProjectTask
            ?? throw new KeyNotFoundException("Task not found");

        await RequirePermissionAsync(task, PermissionAction.Delete, cancellationToken);

        // Archive (soft delete)
        task.Archive();

        return Unit.Value;
    }
}
