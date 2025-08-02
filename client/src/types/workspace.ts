import { Role } from "@/utils/role-utils";
import { UserSummary } from "./user";
import { SpaceSummary } from "./space";

export interface WorkspaceDetail {
    id: string;
    name: string;
    description: string;
    color: string;
    yourRole: Role;
    owner: UserSummary;
    memberCount: number;
    createdAtUtc: string;  // Changed from DateTime to string for ISO date
    joinCode: string | null;
    members: UserSummary[] | null;
    spaces: SpaceSummary[] | null;
}

export interface WorkspaceSummary {
    id: string;
    name: string;
    description: string;
    color: string;
    role: Role;
}