using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Support;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.StatusManagement.UpdateStatus;

public class UpdateStatusHandler : BaseCommandHandler, IRequestHandler<UpdateStatusCommand, Unit>
{
    public UpdateStatusHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateStatusCommand request, CancellationToken cancellationToken)
    {
        var status = await FindOrThrowAsync<Status>(request.StatusId);

        // TODO: Permission check based on status.LayerType and status.LayerId
        
        if (request.Name != null || request.Color != null || request.Category.HasValue)
        {
            status.UpdateDetails(
                newName: request.Name ?? status.Name,
                newColor: request.Color ?? status.Color,
                newCategory: request.Category
            );
        }

        return Unit.Value;
    }
}
