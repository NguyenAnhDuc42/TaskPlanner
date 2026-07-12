import { useEffect, useState } from "react";
import { formatDistanceToNow } from "date-fns";
import { useWorkspaceRootStore } from "@/stores/workspace-root.store";
import { api } from "@/lib/api-client";
import type { ChangeEntry } from "./changes-feed";

interface ChangeEntryDto {
  id: number;
  entityType: string;
  action: "C" | "U" | "D";
  authorMemberId: string;
  createdAt: string;
}

const ACTION_VERBS: Record<ChangeEntryDto["action"], string> = {
  C: "created this",
  U: "updated this",
  D: "deleted this",
};

// Explicitly-requested tier — fetched on panel open, MobX-only, no IndexedDB persistence.
export function useEntityChanges(entityId: string, entityType: "Task" | "Space") {
  const rootStore = useWorkspaceRootStore();
  const [entries, setEntries] = useState<ChangeEntry[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    if (!entityId) return;
    let cancelled = false;

    (async () => {
      setIsLoading(true);
      try {
        const { data } = await api.get<ChangeEntryDto[]>(`/changes/${entityId}`, {
          params: { entityType },
          headers: { "X-Workspace-Id": rootStore.workspaceId },
        });
        if (cancelled) return;
        setEntries(data.map((c) => {
          const author = rootStore.memberStore.getById(c.authorMemberId);
          return {
            id: String(c.id),
            authorName: author?.name ?? "Someone",
            message: ACTION_VERBS[c.action] ?? "updated this",
            timestamp: formatDistanceToNow(new Date(c.createdAt), { addSuffix: true }),
          };
        }));
      } catch (err) {
        console.error(`Failed to fetch changes for ${entityType} ${entityId}:`, err);
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    })();

    return () => { cancelled = true; };
  }, [entityId, entityType, rootStore]);

  return { entries, isLoading };
}
