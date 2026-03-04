using Application.Common.Interfaces;
using Domain.Enums.RelationShip;

namespace Application.Features.ListFeatures.Support.GetTaskCreateLists;

public record GetTaskCreateListsQuery(
    Guid LayerId,
    EntityLayerType LayerType,
    Guid? StatusId = null) : IQuery<List<TaskCreateListOptionDto>>;

public record TaskCreateListOptionDto(
    Guid Id,
    string Name,
    string Color,
    string Icon
);
