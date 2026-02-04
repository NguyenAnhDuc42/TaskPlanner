using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.EntityAccessManagement.AddEntityAccess;

public record AddEntityAccessCommand(
    Guid LayerId,
    EntityLayerType LayerType,
    List<Guid> UserIds,
    AccessLevel AccessLevel
) : ICommand<Unit>;
