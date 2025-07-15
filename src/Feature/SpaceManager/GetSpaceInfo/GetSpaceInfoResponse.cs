namespace src.Feature.SpaceManager.GetSpaceInfo;

public record class TaskList(List<Task> tasks);

public record class Task(Guid id,string name,int priority,DateTime? startDate,DateTime? dueDate);