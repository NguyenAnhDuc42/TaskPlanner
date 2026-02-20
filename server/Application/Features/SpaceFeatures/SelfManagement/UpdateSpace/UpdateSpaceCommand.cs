using System;
using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.SpaceFeatures.SelfManagement.UpdateSpace;

public record UpdateSpaceCommand( Guid SpaceId,
    string? Name,
    string? Description,
    string? Color,
    string? Icon,
    bool? IsPrivate
) : ICommand<Unit>;