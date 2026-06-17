import { z } from "zod";

export const workspaceSearchSchema = z.object({
  contextPanel: z.object({
    type: z.enum(["task", "folder", "space", "project"]),
    id: z.string(),
  }).optional(),
});

export type WorkspaceSearch = z.infer<typeof workspaceSearchSchema>;
