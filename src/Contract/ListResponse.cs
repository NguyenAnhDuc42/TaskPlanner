using src.Domain.Entities.WorkspaceEntity.SupportEntiy;

namespace src.Contract;

public record ListSummary(Guid Id,string Name);

/// Represents the response for a List's board view, with tasks grouped by status.
/// <summary>
/// Represents the response for a List's board view, with tasks grouped by status.
/// This structure is designed to easily build a UI similar to Trello or ClickUp boards.
/// </summary>
/// <param name="Columns">A list of all available statuses for the list, ordered by status type (e.g., Not Started, Active, Done). Each column contains the tasks that belong to it.</param>
public record ListBoardViewResponse(List<StatusColumn> Columns);

public record StatusColumn(Guid StatusId, string Name, string Color, StatusType Type, List<TaskSummary> Tasks);

/// This structure is designed to easily build a UI similar to Trello or ClickUp boards.
/// </summary>
/// <param name="Columns">A list of all available statuses for the list, ordered by status type (e.g., Not Started, Active, Done). Each column contains the tasks that belong to it.</param>

// Represents a node in hierarchical data structure
public record ListNode(Guid Id, string Name);
internal record ListDto(Guid Id, string Name, Guid? SpaceId, Guid? FolderId);