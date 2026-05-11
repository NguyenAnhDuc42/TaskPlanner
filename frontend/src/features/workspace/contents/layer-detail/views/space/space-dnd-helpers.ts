import type { TaskViewData } from "../../layer-detail-types";

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

export function calculateOrderKeys(
  targetIndex: number,
  activeId: string,
  items: any[]
): { previousItemOrderKey: string | undefined; nextItemOrderKey: string | undefined } {
  // Strip the active item from the array in case it's still present
  // (same-column reorder: active item exists; cross-column: it doesn't yet)
  const stripped = items.filter((i) => i.id !== activeId);
 
  // Clamp to valid range
  const clampedIndex = Math.max(0, Math.min(targetIndex, stripped.length));
 
  const previousItemOrderKey: string | undefined = stripped[clampedIndex - 1]?.orderKey;
  const nextItemOrderKey: string | undefined = stripped[clampedIndex]?.orderKey;
 
  return { previousItemOrderKey, nextItemOrderKey };
}