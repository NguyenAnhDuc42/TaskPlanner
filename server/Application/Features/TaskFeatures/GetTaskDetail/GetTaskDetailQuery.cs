namespace Application;

public record GetTaskDetailQuery(Guid TaskId) : IQueryRequest<List<TaskRecord>>, IAuthorizedWorkspaceRequest;
