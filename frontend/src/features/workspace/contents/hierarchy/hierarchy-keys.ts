export const hierarchyKeys = {
  all: ["hierarchy"] as const,
  detail: (workspaceId: string) => [...hierarchyKeys.all, workspaceId] as const,
  membersAccess: (layerType: "space" | "folder" | "list", layerId: string) =>
    [...hierarchyKeys.all, "members-access", layerType, layerId] as const,
};
