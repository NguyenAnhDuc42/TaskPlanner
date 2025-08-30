namespace Domain.Entities.Relationship;

using System;
using Domain.Entities.ProjectEntities;


public class UserProjectFolder
{
    public Guid UserId { get; private set; }
    public User User { get; set; } = null!;
    public Guid ProjectFolderId { get; private set; }
    public ProjectFolder ProjectFolder { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private UserProjectFolder() { } // EF

    private UserProjectFolder(Guid userId, Guid folderId)
    {
        if (userId == Guid.Empty) throw new ArgumentException(nameof(userId));
        if (folderId == Guid.Empty) throw new ArgumentException(nameof(folderId));

        UserId = userId;
        ProjectFolderId = folderId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static UserProjectFolder Create(Guid userId, Guid folderId)
        => new(userId, folderId);
}
