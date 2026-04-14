using Application.Common.Interfaces;

namespace Application.Features.WorkflowFeatures.SetLayerWorkflow;

public record SetLayerWorkflowCommand(
    Guid? SpaceId = null,
    Guid? FolderId = null,
    Guid? WorkflowId = null
) : ICommandRequest;
