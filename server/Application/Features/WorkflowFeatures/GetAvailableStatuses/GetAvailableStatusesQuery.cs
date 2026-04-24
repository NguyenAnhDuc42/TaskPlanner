using Application.Common.Interfaces;
using Application.Common.Results;

namespace Application.Features.WorkflowFeatures;

public record GetAvailableStatusesQuery(
    Guid? SpaceId = null,
    Guid? FolderId = null
) : IQueryRequest<List<StatusResponse>>;
