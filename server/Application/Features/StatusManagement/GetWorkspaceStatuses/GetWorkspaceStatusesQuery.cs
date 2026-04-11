using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Enums;

namespace Application.Features.StatusManagement.GetWorkspaceStatuses;

public record StatusDto(
    Guid Id,
    string Name,
    string Color,
    StatusCategory Category
);

public record GetWorkspaceStatusesQuery(
    Guid WorkspaceId,
    Guid? SpaceId = null,
    Guid? FolderId = null
) : IQueryRequest<List<StatusDto>>;
