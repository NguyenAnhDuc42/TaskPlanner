import { useCallback, useLayoutEffect, useRef } from "react";
import type { SyncEngine } from "@/sync/sync-engine";
import { useDebouncedFlush } from "@/sync/use-debounced-flush";
import type { TaskMutations } from "@/mutations/task.mutations";
import type { TaskRecord } from "@/types/projects/task-record";

export function useDebouncedTaskUpdate(taskMutations: TaskMutations, syncEngine: SyncEngine, taskId: string) {
  const taskIdRef = useRef(taskId);
  useLayoutEffect(() => { taskIdRef.current = taskId; });
  const { scheduleFlush } = useDebouncedFlush(syncEngine);

  return useCallback((patches: Partial<TaskRecord>) => {
    taskMutations.updateLocal(taskIdRef.current, patches).catch((err) => console.error("Failed to apply local task update", err));
    scheduleFlush();
  }, [taskMutations, scheduleFlush]);
}
