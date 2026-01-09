import type { Role } from "@/types/role";
import type { WorkspaceVariant } from "@/types/workspace-variant";

// create-workspace.schema.ts

import { z } from "zod";

export const createWorkspaceSchema = z.object({
  name: z
    .string()
    .trim()
    .min(2, "Name must be at least 2 characters")
    .max(100, "Name must be less than 100 characters"),

  description: z
    .string()
    .trim()
    .max(500, "Description must be less than 500 characters")
    .optional(),

  color: z
    .string()
    .regex(/^#([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$/, "Color must be a valid hex"),

  icon: z.string().min(1, "Icon is required").max(50, "Icon is too long"),

  variant: z.union([
    z.literal("Personal"),
    z.literal("Team"),
    z.literal("Company"),
  ]),

  theme: z.union([z.literal("Light"), z.literal("Dark"), z.literal("System")]),

  strictJoin: z.boolean(),
});

export interface WorkspaceSummary {
  id: string;
  name: string;
  icon: string;
  color: string;
  description: string;
  variant: WorkspaceVariant;
  role: Role;
  memberCount: number;
  isPinned: boolean;
}
