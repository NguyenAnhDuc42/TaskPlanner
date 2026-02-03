using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.Support;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.StatusManagement.ReorderStatus;

public class ReorderStatusHandler : BaseFeatureHandler, IRequestHandler<ReorderStatusCommand, Unit>
{
    public ReorderStatusHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(ReorderStatusCommand request, CancellationToken cancellationToken)
    {
        var status = await FindOrThrowAsync<Status>(request.StatusId);

        // Get the layer entity to check permissions
        var layerEntity = await GetLayer(status.LayerId!.Value, status.LayerType);
        
        status.UpdateOrderKey(request.NewOrderKey);
        
        return Unit.Value;
    }
}
