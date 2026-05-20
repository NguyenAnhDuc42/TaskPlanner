import { api } from "@/lib/api-client";
import { useMutation } from "@tanstack/react-query";
import type { BatchUpdateItemDto } from "./layer-detail-types";

/**
 * Formats and sends batch update to the backend.
 * - statusId: null means "unclassify", undefined means "don't change"
 *   We use a sentinel empty GUID for explicit unclassify since JSON omits undefined.
 */
export async function batchUpdateItems(
  workspaceId: string,
  updates: BatchUpdateItemDto[],
) {
  const UNCLASSIFY_SENTINEL = "00000000-0000-0000-0000-000000000000";

  const formattedUpdates = updates.map((u) => ({
    id: u.id,
    type: u.type,
    // null → send sentinel (explicit unclassify)
    // undefined → omit from JSON (backend keeps existing via ?? t.StatusId)
    // GUID string → send as-is
    statusId: u.statusId === null ? UNCLASSIFY_SENTINEL : u.statusId,
    priority: u.priority,
    orderKey: u.orderKey,
    previousItemOrderKey: u.previousItemOrderKey,
    nextItemOrderKey: u.nextItemOrderKey,
  }));

  return api.post("/spaces/batch-update", {
    workspaceId,
    updates: formattedUpdates,
  });
}

/**
 * TanStack Query mutation hook for batch updates.
 * Ready for future SignalR integration — add onSuccess/onSettled
 * callbacks to invalidate queries or trigger cache sync.
 */
export function useBatchUpdateItems() {
  return useMutation({
    mutationFn: ({ workspaceId, updates }: { workspaceId: string; updates: BatchUpdateItemDto[] }) =>
      batchUpdateItems(workspaceId, updates),
  });
}
