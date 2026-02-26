using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.StatusManagement.SyncStatuses;

public record StatusSyncItem(
    Guid? Id, 
    string Name, 
    string Color, 
    StatusCategory Category, 
    bool IsDeleted = false);

public record SyncStatusesCommand(
    Guid LayerId, 
    EntityLayerType LayerType, 
    List<StatusSyncItem> Statuses) : IRequest<Unit>;
