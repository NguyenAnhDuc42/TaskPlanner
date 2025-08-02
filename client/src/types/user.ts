import { Role } from "@/utils/role-utils";

export interface User {
    id: string;
    name: string;
    email: string;
    avatar?: string;
}

export interface UserSummary {
    id: string;
    name: string;
    email: string;
    role: Role | null;
}