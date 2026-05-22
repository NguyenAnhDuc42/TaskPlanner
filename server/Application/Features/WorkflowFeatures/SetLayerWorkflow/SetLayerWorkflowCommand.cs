namespace Application;

public record SetLayerWorkflowCommand(
    Guid? SpaceId = null,
    Guid? FolderId = null,
    Guid? WorkflowId = null
) : ICommandRequest;


