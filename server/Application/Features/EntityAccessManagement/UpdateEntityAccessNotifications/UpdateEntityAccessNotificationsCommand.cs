using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.EntityAccessManagement.UpdateEntityAccessNotifications;

public record UpdateEntityAccessNotificationsCommand(
    Guid LayerId,
    EntityLayerType LayerType,
    bool NotificationsEnabled
) : ICommand<Unit>;
