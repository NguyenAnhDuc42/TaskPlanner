using Application.Common.Interfaces;
using Application.Features.ViewFeatures; // For TaskViewData

namespace Application.Features.SpaceFeatures;

public record GetSpaceItemsQuery(
    Guid SpaceId
) : IQueryRequest<TaskViewData>, IAuthorizedWorkspaceRequest;
