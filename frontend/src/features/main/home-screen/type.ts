import { z } from "zod";

export const createWorkspaceSchema = z.object({
  name: z.string().trim().min(2).max(100),
  description: z.string().trim().max(500).optional(),
  color: z.string().regex(/^#([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$/),
  icon: z.string().min(1).max(50),
  theme: z.string().default("System"),
  strictJoin: z.boolean(),
});
