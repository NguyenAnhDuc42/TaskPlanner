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
  listAssigneesRoot: () => [...tasksKeys.all, "list-assignees"] as const,
  listAssignees: (workspaceId: string, listId: string) =>
    [...tasksKeys.listAssigneesRoot(), workspaceId, listId] as const,
  assigneesRoot: () => [...tasksKeys.all, "assignees"] as const,
  assignees: (workspaceId: string, taskId: string) =>
    [...tasksKeys.assigneesRoot(), workspaceId, taskId] as const,
  assigneeCandidatesRoot: () => [...tasksKeys.all, "assignee-candidates"] as const,
  assigneeCandidates: (workspaceId: string, taskId: string, search: string) =>
    [...tasksKeys.assigneeCandidatesRoot(), workspaceId, taskId, search] as const,
};
