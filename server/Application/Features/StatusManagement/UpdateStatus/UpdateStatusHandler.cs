using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.Support;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.StatusManagement.UpdateStatus;

public class UpdateStatusHandler : BaseFeatureHandler, IRequestHandler<UpdateStatusCommand, Unit>
{
    public UpdateStatusHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateStatusCommand request, CancellationToken cancellationToken)
    {
        var status = await FindOrThrowAsync<Status>(request.StatusId);
        var layerEntity = await GetLayer(status.LayerId!.Value, status.LayerType);
        
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
