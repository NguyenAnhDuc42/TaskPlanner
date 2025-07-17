using src.Domain.Entities.WorkspaceEntity.SupportEntiy;

namespace src.Feature.ListManager.GetListInfo;

public record class TaskLineList(Dictionary<PlanTaskStatus, List<TaskLineItem>> tasks);

public record class TaskLineItem(Guid id,string name,int priority,PlanTaskStatus status,DateTime? startDate,DateTime? dueDate);
