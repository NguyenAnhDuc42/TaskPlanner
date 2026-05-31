import type { AccessLevel } from "../access-level";

export interface EntityAccessRecord {
    id: string;
    workspaceMemberId: string;
    accessLevel: AccessLevel;
    haveAccess: boolean;
}
