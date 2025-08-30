namespace Domain.Entities.Relationship;

using System;
using Domain.Entities.ProjectEntities;


public class UserProjectSpace
{
    public Guid UserId { get; private set; }
    public User User { get; set; } = null!;
    public Guid ProjectSpaceId { get; private set; }
    public ProjectSpace ProjectSpace { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private UserProjectSpace() { } // EF

    private UserProjectSpace(Guid userId, Guid spaceId)
    {
        if (userId == Guid.Empty) throw new ArgumentException(nameof(userId));
        if (spaceId == Guid.Empty) throw new ArgumentException(nameof(spaceId));

        UserId = userId;
        ProjectSpaceId = spaceId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static UserProjectSpace Create(Guid userId, Guid spaceId)
        => new(userId, spaceId);
}
