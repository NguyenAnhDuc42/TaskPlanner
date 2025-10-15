namespace Domain.Enums.RelationShip;

public enum MembershipStatus
{
    Pending,   // User requested to join with a code, waiting for approval
    Active,    // Approved member
    Invited,   // Invited directly by an admin, hasn't accepted yet
    Suspended  // Member was suspended by an admin
}
