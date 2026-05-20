import { prioritySort } from "@/types/priority";
import type { LayerItem, TaskViewData } from "../../layer-detail-types";

export { calculateOrderKeys } from "../space/space-dnd-helpers";

export function buildColumns(viewData: TaskViewData): Record<string, LayerItem[]> {
  const cols: Record<string, LayerItem[]> = {};
  const statuses = viewData.statuses ?? [];

  statuses.forEach((status) => {
    const folders = (viewData.folders ?? [])
      .filter((f) => f.statusId === status.statusId)
      .map((f) => ({ ...f, __type: "folder" }) satisfies LayerItem);
    const tasks = (viewData.tasks ?? [])
      .filter((t) => t.statusId === status.statusId)
      .map((t) => ({ ...t, __type: "task" }) satisfies LayerItem);
    cols[status.statusId] = [...folders, ...tasks].sort(prioritySort);
  });

  const statusIds = statuses.map((s) => s.statusId);
  const unclassifiedFolders = (viewData.folders ?? [])
    .filter((f) => !f.statusId || !statusIds.includes(f.statusId as string))
    .map((f) => ({ ...f, __type: "folder" }) satisfies LayerItem);
  const unclassifiedTasks = (viewData.tasks ?? [])
    .filter((t) => !t.statusId || !statusIds.includes(t.statusId as string))
    .map((t) => ({ ...t, __type: "task" }) satisfies LayerItem);

  if (unclassifiedFolders.length > 0 || unclassifiedTasks.length > 0) {
    cols["unclassified"] = [...unclassifiedFolders, ...unclassifiedTasks].sort(prioritySort);
  }

  return cols;
}
