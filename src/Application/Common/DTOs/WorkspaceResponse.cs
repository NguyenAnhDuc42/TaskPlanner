using src.Domain.Enums;

namespace src.Application.Common.DTOs;
public record WorkspaceDetail(
    Guid Id,
    string Name,
    string Description,
    string Color,
    string Icon,
    Role YourRole,
    UserSummary Owner,
    int MemberCount,
    DateTime CreatedAtUtc,
    string? JoinCode,
    List<UserSummary>? Members,
    List<SpaceSummary>? Spaces
);

public record WorkspaceSummary(Guid Id, string Name);

// Represents a node in hierarchical data structure
public record Hierarchy(List<SpaceNode> Spaces);
public record SpaceNode(Guid Id, string Name,string Icon,string Color, List<FolderNode> Folders, List<ListNode> DirectLists);
internal record SpaceDto(Guid Id, string Name,string Icon,string Color);
public record FolderNode(Guid Id, string Name,List<ListNode> Lists);
internal record FolderDto(Guid Id, string Name, Guid SpaceId);
public record ListNode(Guid Id, string Name);
internal record ListDto(Guid Id, string Name, Guid? SpaceId, Guid? FolderId);