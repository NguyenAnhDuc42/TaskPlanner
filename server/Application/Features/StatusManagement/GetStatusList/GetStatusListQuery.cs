using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Enums;

namespace Application.Features.StatusManagement.GetStatusList;

public record StatusDto(
    Guid Id,
    string Name,
    string Color,
    StatusCategory Category
);

public record GetStatusListQuery(Guid WorkflowId) : IQueryRequest<List<StatusDto>>;
