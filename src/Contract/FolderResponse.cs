namespace src.Contract;

public record class FolderSummary(Guid Id,string Name);



// Represents a node in hierarchical data structure
public record FolderNode(Guid Id, string Name,List<ListNode> Lists);
internal record FolderDto(Guid Id, string Name, Guid SpaceId);