using Application.Common.Interfaces;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Application.Features.ViewFeatures.CreateView;

public record CreateViewCommand(
    string Name, 
    EntityLayerType LayerType, 
    Guid LayerId, 
    ViewType ViewType
) : ICommandRequest<Guid>;