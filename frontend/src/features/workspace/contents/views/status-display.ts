import type { TaskDto, StatusDto } from "./views-type";

interface StatusGroupBucket {
  statuses: StatusDto[];
  firstIndex: number;
}

interface GroupedStatusDisplay {
  statuses: StatusDto[];
  tasksByStatusId: Record<string, TaskDto[]>;
}

const normalizeName = (name: string) => name.trim().toLowerCase();

const toSemanticKey = (status: StatusDto) =>
  `${status.category}:${normalizeName(status.name)}`;

const pickRepresentative = (statuses: StatusDto[]) =>
  [...statuses]
    .sort((a, b) => {
      if (a.isDefault !== b.isDefault) return a.isDefault ? -1 : 1;
      const byName = a.name.localeCompare(b.name, undefined, {
        sensitivity: "base",
      });
      if (byName !== 0) return byName;
      return a.id.localeCompare(b.id);
    })[0];

export function groupStatusesForDisplay(
  statuses: StatusDto[],
  tasks: TaskDto[],
): GroupedStatusDisplay {
  if (!statuses.length) {
    return { statuses: [], tasksByStatusId: {} };
  }

  const grouped = new Map<string, StatusGroupBucket>();
  statuses.forEach((status, index) => {
    const key = toSemanticKey(status);
    const existing = grouped.get(key);
    if (existing) {
      existing.statuses.push(status);
      return;
    }

    grouped.set(key, {
      statuses: [status],
      firstIndex: index,
    });
  });

  const statusIdToDisplayStatusId = new Map<string, string>();
  const displayStatuses = [...grouped.values()]
    .sort((a, b) => a.firstIndex - b.firstIndex)
    .map((bucket) => {
      const representative = pickRepresentative(bucket.statuses);
      bucket.statuses.forEach((status) => {
        statusIdToDisplayStatusId.set(status.id, representative.id);
      });
      return representative;
    });

  const tasksByStatusId: Record<string, TaskDto[]> = {};
  displayStatuses.forEach((status) => {
    tasksByStatusId[status.id] = [];
  });

  tasks.forEach((task) => {
    if (!task.statusId) return;
    const displayStatusId =
      statusIdToDisplayStatusId.get(task.statusId) ?? task.statusId;
    if (!tasksByStatusId[displayStatusId]) {
      tasksByStatusId[displayStatusId] = [];
    }
    tasksByStatusId[displayStatusId].push(task);
  });

  return {
    statuses: displayStatuses,
    tasksByStatusId,
  };
}
