import type { Role } from "../role";

export interface WorkspaceSnippetRecord {
    id: string;
    name: string;
    icon?: string;
    color?: string;
    role?: Role;
    isPinned?: boolean;
    memberCount?: number;
}
