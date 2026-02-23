using System;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.StatusManagement.CreateStatus;

public record CreateStatusCommand(
    Guid LayerId,
    EntityLayerType LayerType,
    string Name,
    string Color,
    StatusCategory Category
) : IRequest<Guid>;
