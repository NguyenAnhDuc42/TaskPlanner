public abstract class Entity<TId> where TId : struct
{
    public TId Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    
    protected Entity() { }
    protected Entity(TId id) : this() => Id = id;
    
    protected void UpdateTimestamp() => UpdatedAt = DateTimeOffset.UtcNow;
    
    public override bool Equals(object? obj) => 
        obj is Entity<TId> other && Id.Equals(other.Id);
    public override int GetHashCode() => Id.GetHashCode();
}
