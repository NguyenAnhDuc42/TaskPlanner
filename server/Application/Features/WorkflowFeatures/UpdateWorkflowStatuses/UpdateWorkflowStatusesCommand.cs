using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Features.WorkflowFeatures.UpdateWorkflowStatuses;

public record UpdateWorkflowStatusesCommand(
    Guid WorkflowId,
    List<StatusUpdateDto> Statuses
) : ICommandRequest;

public record StatusUpdateDto(
    Guid? Id, // If null, create new status
    string Name,
    string Color,
    StatusCategory Category
);
