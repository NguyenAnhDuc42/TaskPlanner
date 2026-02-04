using Application.Common.Filters;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Enums.RelationShip;

namespace Application.Features.EntityAccessManagement.GetEntityAccessList;

public record GetEntityAccessListQuery(
    Guid LayerId,
    EntityLayerType LayerType
) : IQuery<List<EntityAccessDto>>;
