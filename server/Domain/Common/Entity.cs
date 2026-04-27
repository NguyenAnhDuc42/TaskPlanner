namespace Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset UpdatedAt { get; protected set; }
    public Guid? CreatorId { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    
    protected Entity() 
    { 
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }
    
    protected Entity(Guid id) 
    {
        Id = id;
        var now = DateTimeOffset.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }

    protected void InitializeAudit(Guid? creatorId)
    {
        CreatorId = creatorId;
    }

    protected void UpdateTimestamp() => UpdatedAt = DateTimeOffset.UtcNow;
    
    public override bool Equals(object? obj) => obj is Entity other && Id.Equals(other.Id);
    public override int GetHashCode() => Id.GetHashCode();

    public void SoftDelete() 
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdateTimestamp();
    }
}
