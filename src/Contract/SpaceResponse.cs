namespace src.Contract;

public record class Space();

// Represents a node in hierarchical data structure
public record SpaceNode(Guid Id, string Name, List<FolderNode> Folders, List<ListNode> DirectLists);
internal record SpaceDto(Guid Id, string Name);