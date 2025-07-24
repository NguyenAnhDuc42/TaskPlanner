import { Role } from "@/utils/role-utils";

export interface User {
    id: string;
    name: string;
    email: string;
    avatar?: string;
}

export interface Members{
    members: Member[];
}
export interface Member{
    id: string;
    name: string;
    email: string;
    role: Role;
}
