namespace Api;

public record TaskRecord
{
    public Guid Id { get; init; }
    public Guid? WorkspaceId { get; init; }
    public string Name { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? StatusId { get; init; }
    public Priority? Priority { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public string? OrderKey { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }

    // Ancestor ids
    public Guid? SpaceId { get; init; }
    public Guid? FolderId { get; init; }

    // Detailed properties
    public Guid? DefaultDocumentId { get; init; }
    public bool? IsArchived { get; init; }
    public int? StoryPoints { get; init; }
    public long? TimeEstimateSeconds { get; init; }
    public string? ParentType { get; init; }
    public Guid? ParentTaskId { get; init; }
    public AccessLevel? AccessLevel { get; init; }
}

public record FolderRecord
{
    public Guid Id { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? SpaceId { get; init; }
    public string Name { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public string? OrderKey { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public bool? HasTasks { get; init; }
    public AccessLevel? AccessLevel { get; init; }
}

public record SpaceRecord
{
    public Guid Id { get; init; }
    public Guid WorkspaceId { get; init; }   // ancestor id for client-side workspace scoping
    public string Name { get; init; } = null!;
    public string? Color { get; init; }
    public string? Icon { get; init; }
    public bool IsPrivate { get; init; }
    public string? OrderKey { get; init; }
    public Guid? DefaultDocumentId { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public Guid? CreatorId { get; init; }
    public bool? HasFolders { get; init; }
    public bool? HasTasks { get; init; }
    public AccessLevel? AccessLevel { get; init; }
}

public record DocumentRecord
{
    public Guid Id { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid SpaceId { get; init; }
    public Guid? ParentDocumentId { get; init; }
    public string Name { get; init; } = null!;
    public string? OrderKey { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? LastEditedAt { get; init; }
}

public record DocumentBlockRecord
{
    public Guid Id { get; init; }
    public Guid DocumentId { get; init; }
    public BlockType Type { get; init; }
    public string Content { get; init; } = null!;
    public string OrderKey { get; init; } = null!;
}

public record FavoriteRecord
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public string EntityLayerType { get; init; } = null!;
    public string OrderKey { get; init; } = null!;
}

public record CommentRecord
{
    public Guid Id { get; init; }
    public string Content { get; init; } = null!;
    public Guid CreatorId { get; init; }
    public Guid? TaskId { get; init; }
    public Guid? ParentCommentId { get; init; }
    public bool IsEdited { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public record AssigneeRecord
{
    public Guid Id { get; init; }
    public Guid TaskId { get; init; }
    public Guid WorkspaceMemberId { get; init; }
}

public record ChangeEntryRecord
{
    public long Id { get; init; }
    public string EntityType { get; init; } = null!;
    public string Action { get; init; } = null!;
    public Guid AuthorMemberId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public record StatusRecord
{
    public Guid Id { get; init; }
    public Guid? SpaceId { get; init; }
    public string Name { get; init; } = null!;
    public string? Color { get; init; }
    public string? OrderKey { get; init; }
}

public record WorkspaceRecord
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public string? Description { get; init; }

    public Role? Role { get; init; }
    public bool? IsPinned { get; init; }
    public bool? IsArchived { get; init; }
    public MembershipStatus MembershipStatus { get; init; } = MembershipStatus.Active;

    // Invite — only populated for Admin/Owner
    public string? JoinCode { get; init; }
    public bool StrictJoin { get; init; }
}

public record MemberRecord
{
    public Guid Id { get; init; }       // WorkspaceMember.Id — workspace-scoped identity
    public Guid? UserId { get; init; }  // User.Id — for lookups by user (e.g. comment.creatorId)
    public string Name { get; init; } = null!;
    public string? Email { get; init; }
    public string? AvatarUrl { get; init; }
    public Role? Role { get; init; }
    public MembershipStatus? Status { get; init; }
    public DateTimeOffset? CreatedAt { get; init; }
    public DateTimeOffset? JoinedAt { get; init; }

    public static MemberRecord FromDomain(WorkspaceMember wm, User user) => new()
    {
        Id = wm.Id,
        UserId = user.Id,
        Name = user.Name,
        Email = user.Email,
        Role = wm.Role,
        Status = wm.Status,
        CreatedAt = wm.CreatedAt,
        JoinedAt = wm.JoinedAt
    };
}

public record NotificationRecord
{
    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public string Title { get; init; } = null!;
    public string? Body { get; init; }
    public bool IsRead { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    // Actor info for display
    public string? ActorName { get; init; }
}
