import type { AccessLevel } from "../access-level";


export interface EntityAccessRecord {
    workspaceMemberId: string;
    accessLevel: AccessLevel;
    haveAccess: boolean;
}
