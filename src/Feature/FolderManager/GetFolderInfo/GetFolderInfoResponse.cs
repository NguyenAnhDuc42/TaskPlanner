namespace src.Feature.FolderManager.GetFolderInfo;

public record class TaskList(List<Task> tasks);

public record class Task(Guid id,string name,int priority,DateTime? startDate,DateTime? dueDate);