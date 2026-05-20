using Application.Common.Interfaces;
using Domain.Enums.RelationShip;

namespace Application.Features.ItemFeatures;

public record BatchUpdateItemsCommand(
    Guid WorkspaceId,
    List<BatchUpdateItemDto> Updates
) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record BatchUpdateItemDto(
    Guid Id,
    EntityLayerType Type, // ProjectTask or ProjectFolder
    Guid? StatusId,
    string? Priority,
    string? OrderKey,
    string? PreviousItemOrderKey = null,
    string? NextItemOrderKey = null
);
