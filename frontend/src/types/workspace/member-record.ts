import type { MembershipStatus } from "../membership-status";
import type { Role } from "../role";


export interface MemberRecord {
    id: string;
    workspaceMemberId?: string;
    name: string;
    email?: string;
    avatarUrl?: string;
    role?: Role;
    status?: MembershipStatus;
    createdAt?: string; // ISO 8601 string
    joinedAt?: string;  // ISO 8601 string
}
