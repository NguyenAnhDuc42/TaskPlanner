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
  members: z
    .array(
      z.object({
        email: z.string().email("Invalid email format"),
        role: z.enum(["Admin", "Member", "Guest"]), // match your Role enum
      }),
    )
    .min(1, "At least one member required"),

  enableEmail: z.boolean().optional(),
  message: z.string().max(500).optional(),
});
