import type { Role } from "@/types/role";
import z from "zod";

export interface MemberSummary {
    id: string;
    name: string;
    email: string;
    avatarUrl: string;
    role: Role;
    createdAt: string;
    joinedAt: string;
}

export const addMembersSchema = z.object({

  emails: z
    .array(z.string().email("Invalid email"))
    .min(1, "At least one email is required"),
  role: z.union([
    z.literal("Guest"),
    z.literal("Member"),
    z.literal("Admin"),
  ]),
});