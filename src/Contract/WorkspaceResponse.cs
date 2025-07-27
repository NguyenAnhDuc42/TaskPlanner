namespace src.Contract;

public record Workspace(Guid Id, string Name, string Description, string JoinCode, string Color, bool IsPrivate, UserSummary Creator);
public record WorkspaceSummary(Guid Id, string Name);

// Represents a node in hierarchical data structure
public record Hierarchy(List<SpaceNode> Spaces);

