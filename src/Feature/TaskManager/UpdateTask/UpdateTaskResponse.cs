

using src.Contract;
using src.Domain.Entities.WorkspaceEntity.SupportEntiy;

namespace src.Feature.TaskManager.UpdateTask;

public record class UpdateTaskResponse(TaskDetail task, string message);  


