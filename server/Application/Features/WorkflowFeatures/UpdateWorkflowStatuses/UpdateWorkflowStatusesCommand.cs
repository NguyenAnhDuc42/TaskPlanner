namespace Application;

public record UpdateWorkflowStatusesCommand(
    Guid WorkflowId,
    List<StatusUpdateRecord> Statuses
) : ICommandRequest, IAuthorizedWorkspaceRequest;


