import { api } from "@/lib/api-client";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import type { BatchUpdateItemDto } from "./layer-detail-types";

export async function batchUpdateItems(
  workspaceId: string,
  updates: BatchUpdateItemDto[],
) {
  const formattedUpdates = updates.map((u) => ({
    id: u.id,
    type: u.type,
    statusId: u.statusId === "unclassified" ? null : u.statusId,
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


export function useBatchUpdateItems() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ workspaceId, updates }: { workspaceId: string; updates: BatchUpdateItemDto[] }) =>
      batchUpdateItems(workspaceId, updates),
    onSettled: () => {
    },
  });
}
