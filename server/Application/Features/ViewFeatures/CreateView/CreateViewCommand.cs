using Application.Common.Interfaces;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Application.Features.ViewFeatures.CreateView;

public record CreateViewCommand(
    Guid LayerId,
    EntityLayerType LayerType,
    ViewType ViewType,
    string Name
) : ICommand<Guid>;