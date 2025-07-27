namespace src.Contract;

public record class SpaceSummary(Guid Id,string Name);

// Represents a node in hierarchical data structure
public record SpaceNode(Guid Id, string Name, List<FolderNode> Folders, List<ListNode> DirectLists);
internal record SpaceDto(Guid Id, string Name);