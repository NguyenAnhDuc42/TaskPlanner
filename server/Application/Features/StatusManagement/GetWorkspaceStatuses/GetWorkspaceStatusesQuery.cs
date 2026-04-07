using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Features.StatusManagement.GetWorkspaceStatuses;

public record StatusDto(
    Guid Id,
    string Name,
    string Color,
    StatusCategory Category
);

public record GetWorkspaceStatusesQuery() : IQuery<List<StatusDto>>;
