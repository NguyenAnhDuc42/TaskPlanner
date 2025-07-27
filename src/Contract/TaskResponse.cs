namespace src.Contract;

public record class TaskSummary(Guid Id, string Name, DateTime? DueDate, int Priority, List<UserSummary> assignees);
