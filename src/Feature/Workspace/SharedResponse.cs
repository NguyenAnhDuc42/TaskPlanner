using src.Domain.Entities.WorkspaceEntity.SupportEntiy;

namespace src.Feature.Workspace;

public record Members(List<Member> members);
public record Member(Guid Id, string Name, string Email, string Role);

public record TaskList(IDictionary<PlanTaskStatus, List<Task>> tasks);
public record Tasks(List<Task> tasks);
public record Task(Guid Id, string Name,DateTime DueDate, PlanTaskStatus Status,int priority,List<Assignee> assignees);
public record Assignee(Guid Id, string Name, string Email);
