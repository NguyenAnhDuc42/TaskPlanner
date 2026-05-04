import z from "zod";

export interface CommandCenterStats {
  activeProjects: number;
  pendingTasks: number;
  velocity: number;
  health: string;
}

// Add any schemas if needed for command center actions
export const commandCenterFilterSchema = z.object({
  timeRange: z.enum(["7d", "30d", "90d", "all"]).default("30d"),
});
