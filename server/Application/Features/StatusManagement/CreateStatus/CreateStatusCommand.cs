using System;
using Domain.Enums;
using MediatR;

namespace Application.Features.StatusManagement.CreateStatus;

public record CreateStatusCommand(
    Guid LayerId,
    EntityLayerType LayerType,
    string Name,
    string Color,
    StatusCategory Category,
    long? OrderKey = null
) : IRequest<Guid>;
