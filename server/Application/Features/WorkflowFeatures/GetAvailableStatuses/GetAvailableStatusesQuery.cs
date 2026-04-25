using Application.Common.Interfaces;
using Application.Common.Results;

using Domain.Enums;

namespace Application.Features.WorkflowFeatures;

public record GetAvailableStatusesQuery(
    Guid? SpaceId = null,
    Guid? FolderId = null
) : IQueryRequest<List<StatusResponse>>;

public record StatusResponse(
    Guid Id,
    string Name,
    string Color,
    StatusCategory Category
);
