using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.EntityMemberManagement.UpdateEntityMemberNotifications;

public record UpdateEntityMemberNotificationsCommand(
    Guid LayerId,
    EntityLayerType LayerType,
    bool NotificationsEnabled
) : ICommand<Unit>;
