using MediatR;
using Domain.Enums;

namespace Application.Features.StatusManagement.GetStatusList;

public record GetStatusListQuery(Guid WorkflowId) : IRequest<List<StatusDto>>;

public record StatusDto(
    Guid Id,
    string Name,
    string Color,
    StatusCategory Category
);
