using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.ListFeatures.SelfManagement.DeleteList;

public class DeleteListHandler : BaseCommandHandler, IRequestHandler<DeleteListCommand, Unit>
{
    public DeleteListHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteListCommand request, CancellationToken cancellationToken)
    {
        var list = await FindOrThrowAsync<ProjectList>(request.ListId);

        await RequirePermissionAsync(list, PermissionAction.Delete, cancellationToken);

        // Check if list has active tasks
        var hasTasks = await UnitOfWork.Set<ProjectTask>()
            .AnyAsync(t => t.ProjectListId == list.Id && !t.IsArchived, cancellationToken);

        if (hasTasks)
        {
            throw new InvalidOperationException("Cannot delete list that contains active tasks. Archive or move the tasks first.");
        }

        // Archive (soft delete)
        list.Archive();

        return Unit.Value;
    }
}
