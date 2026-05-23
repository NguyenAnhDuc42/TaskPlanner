namespace Application;

public record GetTaskDetailQuery(Guid TaskId) : IQueryRequest<TaskRecord>, IAuthorizedWorkspaceRequest;


