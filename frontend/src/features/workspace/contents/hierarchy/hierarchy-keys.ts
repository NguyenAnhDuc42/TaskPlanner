export const hierarchyKeys = {
  all: ["hierarchy"] as const,
  detail: (workspaceId: string) => [...hierarchyKeys.all, workspaceId, "structure"] as const,
  nodeTasks: (workspaceId: string, nodeId: string) => [...hierarchyKeys.all, workspaceId, "node", nodeId, "tasks"] as const,
  membersAccess: (
    layerType: "space" | "folder",
    layerId: string,
    isManagementMode: boolean = false,
  ) =>
    [
      ...hierarchyKeys.all,
      "members-access",
      layerType,
      layerId,
      isManagementMode,
    ] as const,
};
