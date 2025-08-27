namespace Domain.Entities.Relationship;

using System;
using Domain.Entities.ProjectEntities;


public class UserProjectList
{
    public Guid UserId { get; private set; }
    public User User { get; set; } = null!;
    public Guid ProjectListId { get; private set; }
    public ProjectList ProjectList { get; set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private UserProjectList() { } // EF

    private UserProjectList(Guid userId, Guid listId)
    {
        if (userId == Guid.Empty) throw new ArgumentException(nameof(userId));
        if (listId == Guid.Empty) throw new ArgumentException(nameof(listId));

        UserId = userId;
        ProjectListId = listId;
        CreatedAt = DateTime.UtcNow;
    }

    public static UserProjectList Create(Guid userId, Guid listId)
        => new(userId, listId);
}
