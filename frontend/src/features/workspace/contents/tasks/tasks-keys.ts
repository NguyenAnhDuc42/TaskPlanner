export const tasksKeys = {
  all: ["tasks"] as const,
  detail: (taskId: string) => [...tasksKeys.all, "detail", taskId] as const,
  createListOptionsRoot: () => [...tasksKeys.all, "create-list-options"] as const,
  createListOptions: (
    workspaceId: string,
    layerId: string,
    layerType: string,
    statusId?: string,
  ) =>
    [
      ...tasksKeys.createListOptionsRoot(),
      workspaceId,
      layerId,
      layerType,
      statusId ?? null,
    ] as const,
};
