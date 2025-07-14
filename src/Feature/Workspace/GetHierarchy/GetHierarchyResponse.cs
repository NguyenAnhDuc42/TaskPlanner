namespace src.Feature.Workspace.GetHierarchy;

public record Hierarchy(List<SpaceNode> spaces);



public record SpaceNode(Guid id, string name, List<FolderNode> Folders, List<ListNode> DirectLists);
public record FolderNode(Guid id, string name,List<ListNode> Lists);
public record ListNode(Guid id, string name);


internal record SpaceDto(Guid Id, string Name);
internal record FolderDto(Guid Id, string Name, Guid SpaceId);
internal record ListDto(Guid Id, string Name, Guid? SpaceId, Guid? FolderId);

