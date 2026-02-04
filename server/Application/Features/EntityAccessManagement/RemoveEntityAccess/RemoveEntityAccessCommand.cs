using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.EntityAccessManagement.RemoveEntityAccess;

public record RemoveEntityAccessCommand(
    Guid LayerId,
    EntityLayerType LayerType,
    List<Guid> UserIds
) : ICommand<Unit>;
