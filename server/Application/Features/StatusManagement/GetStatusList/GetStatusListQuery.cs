using System;
using Application.Common.Results;
using Application.Contract.StatusContract;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.StatusManagement.GetStatusList;

public record GetStatusListQuery(
    Guid LayerId,
    EntityLayerType LayerType
) : IRequest<List<StatusDto>>;
