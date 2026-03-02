using System;
using Application.Common.Interfaces;
using Application.Contract.StatusContract;
using Domain.Enums.RelationShip;

namespace Application.Features.StatusManagement.GetStatusList;

public record GetStatusListQuery(
    Guid LayerId,
    EntityLayerType LayerType
) : IQuery<List<StatusDto>>;
