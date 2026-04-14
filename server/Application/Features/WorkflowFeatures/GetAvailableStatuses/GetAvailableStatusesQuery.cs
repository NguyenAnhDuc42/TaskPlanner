using Application.Common.Interfaces;
using Application.Features.WorkflowFeatures.Common;
using Application.Common.Results;

namespace Application.Features.WorkflowFeatures.GetAvailableStatuses;

public record GetAvailableStatusesQuery(
    Guid? SpaceId = null,
    Guid? FolderId = null
) : IQueryRequest<List<StatusResponse>>;
