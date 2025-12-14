import { Role } from "@/utils/role-utils";

export interface UserDetail {
    id: string;
    name: string;
    email: string;

}

export interface UserSummary {
    id: string;
    name: string;
    email: string;
    role: Role | null;
}