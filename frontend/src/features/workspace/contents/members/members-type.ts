import type { Role } from "@/types/role";
import type { MembershipStatus } from "@/types/membership-status";
import z from "zod";

export interface MemberSummary {
  id: string;
  workspaceMemberId: string;
  name: string;
  email: string;
  avatarUrl: string;
  role: Role;
  status?: MembershipStatus;
  createdAt: string;
  joinedAt: string;
}

export const addMembersSchema = z.object({
  members: z
    .array(
      z.object({
        email: z.string().email("Invalid email format"),
        role: z.custom<Role>(), // match your Role enum
      }),
    )
    .min(1, "At least one member required"),

  enableEmail: z.boolean().optional(),
  message: z.string().max(500).optional(),
});

export const updateMembersSchema = z.object({
  members: z
    .array(
      z.object({
        userId: z.uuid(),
        role: z.custom<Role>().optional(),
        status: z.custom<MembershipStatus>().optional(),
      }),
    )
    .min(1, "At least one member required"),
});
