using src.Domain.Entities.WorkspaceEntity.SupportEntiy;

namespace src.Contract;

public record ListSummary(Guid Id,string Name);


public record StatusColumn(Guid StatusId, string Name, string Color, StatusType Type, List<TaskSummary> Tasks);

// Represents a node in hierarchical data structure
public record ListNode(Guid Id, string Name);
internal record ListDto(Guid Id, string Name, Guid? SpaceId, Guid? FolderId);