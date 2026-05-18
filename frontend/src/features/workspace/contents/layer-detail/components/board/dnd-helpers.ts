import type { TaskViewData } from "../../layer-detail-types";

const lexSort = (a: any, b: any) => {
  const ak = a.orderKey ?? "";
  const bk = b.orderKey ?? "";
  return ak < bk ? -1 : ak > bk ? 1 : 0;
};

export function buildColumns(viewData: TaskViewData): Record<string, any[]> {
  const cols: Record<string, any[]> = {};
  const statuses = viewData.statuses ?? [];
  const statusIds = new Set(statuses.map((s) => s.statusId));

  // Initialize empty columns for each known status
  statuses.forEach((s) => {
    cols[s.statusId] = [];
  });
  cols["unclassified"] = [];

  // Single-pass classification of folders
  (viewData.folders ?? []).forEach((f) => {
    const key = f.statusId && statusIds.has(f.statusId) ? f.statusId : "unclassified";
    cols[key].push(f);
  });

  // Single-pass classification of tasks
  (viewData.tasks ?? []).forEach((t) => {
    const key = t.statusId && statusIds.has(t.statusId) ? t.statusId : "unclassified";
    cols[key].push(t);
  });

  // Sort each column once by orderKey
  Object.keys(cols).forEach((colId) => {
    cols[colId].sort(lexSort);
  });

  // Clean up unclassified column if empty
  if (cols["unclassified"].length === 0) {
    delete cols["unclassified"];
  }

  return cols;
}

export function calculateOrderKeys(
  targetIndex: number,
  activeId: string,
  items: any[]
): { previousItemOrderKey: string | undefined; nextItemOrderKey: string | undefined } {
  // Strip the active item from the array in case it's still present
  const stripped = items.filter((i) => i.id !== activeId);
 
  // Clamp to valid range
  const clampedIndex = Math.max(0, Math.min(targetIndex, stripped.length));
 
  const previousItemOrderKey: string | undefined = stripped[clampedIndex - 1]?.orderKey;
  const nextItemOrderKey: string | undefined = stripped[clampedIndex]?.orderKey;
 
  return { previousItemOrderKey, nextItemOrderKey };
}
