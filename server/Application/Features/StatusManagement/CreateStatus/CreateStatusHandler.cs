using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.Support;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.StatusManagement.CreateStatus;

public class CreateStatusHandler : BaseFeatureHandler, IRequestHandler<CreateStatusCommand, Guid>
{
    public CreateStatusHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Guid> Handle(CreateStatusCommand request, CancellationToken cancellationToken)
    {
        var layerEntity = await GetLayer(request.LayerId, request.LayerType);
        
        var orderKey = request.OrderKey ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        var status = Status.Create(
            layerId: request.LayerId,
            layerType: request.LayerType,
            name: request.Name,
            color: request.Color,
            category: request.Category,
            orderKey: orderKey,
            creatorId: CurrentUserId
        );

        await UnitOfWork.Set<Status>().AddAsync(status, cancellationToken);
        
        return status.Id;
    }
}
