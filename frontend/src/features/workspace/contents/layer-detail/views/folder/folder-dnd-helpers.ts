import type { TaskViewData } from "../../layer-detail-types";
export { calculateOrderKeys } from "../space/space-dnd-helpers";

const lexSort = (a: any, b: any) => {
  const ak = a.orderKey ?? "";
  const bk = b.orderKey ?? "";
  return ak < bk ? -1 : ak > bk ? 1 : 0;
};

export function buildColumns(viewData: TaskViewData): Record<string, any[]> {
  const cols: Record<string, any[]> = {};
  const statuses = viewData.statuses ?? [];

  statuses.forEach((status) => {
    const folders = (viewData.folders ?? []).filter(
      (f) => f.statusId === status.statusId,
    );
    const tasks = (viewData.tasks ?? []).filter(
      (t) => t.statusId === status.statusId,
    );
    cols[status.statusId] = [...folders, ...tasks].sort(lexSort);
  });

  const statusIds = statuses.map((s) => s.statusId);
  const unclassifiedFolders = (viewData.folders ?? []).filter(
    (f) => !f.statusId || !statusIds.includes(f.statusId as string),
  );
  const unclassifiedTasks = (viewData.tasks ?? []).filter(
    (t) => !t.statusId || !statusIds.includes(t.statusId as string),
  );

  if (unclassifiedFolders.length > 0 || unclassifiedTasks.length > 0) {
    cols["unclassified"] = [...unclassifiedFolders, ...unclassifiedTasks].sort(lexSort);
  }

  return cols;
}