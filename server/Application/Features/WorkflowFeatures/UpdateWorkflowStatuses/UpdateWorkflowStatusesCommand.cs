using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Features.WorkflowFeatures;

public record UpdateWorkflowStatusesCommand(
    Guid WorkflowId,
    List<StatusUpdateDto> Statuses
) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record StatusUpdateDto(
    Guid? Id, 
    string Name,
    string Color,
    StatusCategory Category
);
