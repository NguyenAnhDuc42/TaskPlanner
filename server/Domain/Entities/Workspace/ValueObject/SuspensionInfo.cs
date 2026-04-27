namespace Domain.Entities;

public readonly record struct SuspensionInfo
{
    public DateTimeOffset? SuspendedAt { get; init; }
    public Guid? SuspendedBy { get; init; }

    public bool IsSuspended => SuspendedAt.HasValue;

    private SuspensionInfo(DateTimeOffset? suspendedAt, Guid? suspendedBy)
    {
        SuspendedAt = suspendedAt;
        SuspendedBy = suspendedBy;
    }

    public static SuspensionInfo None => new(null, null);

    public static SuspensionInfo Create(Guid suspenderId) => new(DateTimeOffset.UtcNow, suspenderId);
}
