export const hierarchyKeys = {
  all: ["hierarchy"] as const,
  detail: (workspaceId: string) => [...hierarchyKeys.all, workspaceId] as const,
  membersAccess: (
    layerType: "space" | "folder" | "list",
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
