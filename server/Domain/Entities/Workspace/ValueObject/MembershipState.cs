using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Domain.Entities;

public readonly record struct MembershipState
{
    public MembershipStatus Status { get; init; }
    public DateTimeOffset? JoinedAt { get; init; }

    public MembershipState(MembershipStatus status, DateTimeOffset? joinedAt = null)
    {
        Status = status;
        JoinedAt = joinedAt;
    }

    public static MembershipState Active() => new(MembershipStatus.Active, DateTimeOffset.UtcNow);
    public static MembershipState Pending() => new(MembershipStatus.Pending, null);
    public static MembershipState Invited() => new(MembershipStatus.Invited, null);

    public bool IsActive => Status == MembershipStatus.Active;
}
