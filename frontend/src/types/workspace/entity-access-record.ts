import type { AccessLevel } from "../access-level";

export interface EntityAccessRecord {
    id: string;
    spaceId: string;
    workspaceMemberId: string;
    accessLevel: AccessLevel;
    haveAccess: boolean;
}
