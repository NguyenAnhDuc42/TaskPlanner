export const hierarchyKeys = {
  all: ["hierarchy"] as const,
  detail: (workspaceId: string) => [...hierarchyKeys.all, workspaceId.toLowerCase(), "structure"] as const,
  nodeFolders: (workspaceId: string, nodeId: string) => [...hierarchyKeys.all, workspaceId.toLowerCase(), "node", nodeId.toLowerCase(), "folders"] as const,
  nodeTasks: (workspaceId: string, nodeId: string) => [...hierarchyKeys.all, workspaceId.toLowerCase(), "node", nodeId.toLowerCase(), "tasks"] as const,
  nodeBase: (workspaceId: string) => [...hierarchyKeys.all, workspaceId.toLowerCase(), "node"] as const,
  membersAccess: (
    layerType: "space" | "folder",
    layerId: string,
    isManagementMode: boolean = false,
  ) =>
    [
      ...hierarchyKeys.all,
      "members-access",
      layerType,
      layerId.toLowerCase(),
      isManagementMode,
    ] as const,
};
