namespace src.Contract;

public record ListSumary(Guid Id,string Name);

// Represents a node in hierarchical data structure
public record ListNode(Guid Id, string Name);
internal record ListDto(Guid Id, string Name, Guid? SpaceId, Guid? FolderId);