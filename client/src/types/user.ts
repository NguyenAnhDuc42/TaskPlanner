export interface User {
    id: string;
    name: string;
    email: string;
    avatar?: string;
}
export type Role = "owner" | "admin" | "member";




export interface Members{
    members: Member[];
}
export interface Member{
    id: string;
    name: string;
    email: string;
    role: Role;
}
