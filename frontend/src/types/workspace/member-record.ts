import type { MembershipStatus } from "../membership-status";
import type { Role } from "../role";

export interface MemberRecord {
    id: string;
    userId?: string;
    name: string;
    email?: string;
    avatarUrl?: string;
    role?: Role;
    status?: MembershipStatus;
    createdAt?: string;
    joinedAt?: string;
}
