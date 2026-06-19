import type { MembershipStatus } from "../membership-status";
import type { Role } from "../role";

export interface MemberRecord {
    id: string;       // WorkspaceMember.Id — workspace-scoped identity
    userId?: string;  // User.Id — for lookups that need the actual user (e.g. comment.creatorId)
    name: string;
    email?: string;
    avatarUrl?: string;
    role?: Role;
    status?: MembershipStatus;
    createdAt?: string;
    joinedAt?: string;
}
