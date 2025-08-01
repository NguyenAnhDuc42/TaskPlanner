using src.Domain.Enums;

namespace src.Contract;

public record WorkspaceDetail(
     Guid Id,
    string Name,
    string Description,
    string Color,
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

