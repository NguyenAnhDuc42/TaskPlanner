// Groups statuses and maps tasks to their status IDs for display purposes

export type GroupedStatus = {
  id: string;
  name: string;
  color: string;
  category: string;
};

export function groupStatusesForDisplay(
  statuses: { id: string; name: string; color: string; category: string }[],
  tasks: { id: string; statusId: string }[]
) {
  const tasksByStatusId: Record<string, typeof tasks> = {};

  for (const status of statuses) {
    tasksByStatusId[status.id] = [];
  }

  for (const task of tasks) {
    if (tasksByStatusId[task.statusId]) {
      tasksByStatusId[task.statusId].push(task);
    }
  }

  return {
    statuses: statuses as GroupedStatus[],
    tasksByStatusId,
  };
}
