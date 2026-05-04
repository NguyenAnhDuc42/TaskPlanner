export const commandCenterKeys = {
  all: ["command-center"] as const,
  stats: (workspaceId: string) => [...commandCenterKeys.all, "stats", workspaceId] as const,
};
