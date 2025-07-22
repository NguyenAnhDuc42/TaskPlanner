using src.Domain.Entities.WorkspaceEntity.SupportEntiy;
using src.Domain.Enums;

namespace src.Feature.Workspace;

public record Members(List<Member> members);
public record Member(Guid id, string name, string email, Role role);


public record FolderItems(List<FolderItem> folders);
public record FolderItem(Guid id, string name);


public record ListItems(List<ListItem> items);
public record ListItem(Guid id, string Name);


public record ListOfTaskItems(IDictionary<PlanTaskStatus, List<Task>> tasks);
public record TaskItems(List<TaskItem> tasks);
public record TaskItem(Guid id, string name,DateTime? dueDate, PlanTaskStatus status,int priority,List<Assignee> assignees);
public record Assignee(Guid id, string name, string email);
