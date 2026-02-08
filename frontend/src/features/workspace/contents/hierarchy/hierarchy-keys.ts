export const hierarchyKeys = {
  all: ["hierarchy"] as const,
  detail: (workspaceId: string) => [...hierarchyKeys.all, workspaceId] as const,
};
