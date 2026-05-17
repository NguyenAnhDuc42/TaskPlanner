import { createContext, useContext, useCallback, useRef, useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { workspaceKeys } from "@/features/main/query-keys";
import { useTaskDetail, useUpdateTask } from "./task-api";
import type { EnrichedTaskDetailDto, UpdateTaskRequest } from "./task-types";

interface TaskEditorContextType {
  task: EnrichedTaskDetailDto;
  updateField: (updates: Omit<UpdateTaskRequest, "taskId">) => void;
  isSaving: boolean;
}

const TaskEditorContext = createContext<TaskEditorContextType | null>(null);

interface TaskEditorProviderProps {
  workspaceId: string;
  taskId: string;
  children: React.ReactNode;
}

export function TaskEditorProvider({ workspaceId, taskId, children }: TaskEditorProviderProps) {
  const { data: task, isLoading, isError } = useTaskDetail(workspaceId, taskId);
  const queryClient = useQueryClient();
  const updateTask = useUpdateTask();
  const isSaving = updateTask.isPending;

  const pendingUpdatesRef = useRef<Omit<UpdateTaskRequest, "taskId">>({});
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Helper to commit any pending changes instantly
  const flushPendingUpdates = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
    const finalUpdates = pendingUpdatesRef.current;
    if (Object.keys(finalUpdates).length > 0) {
      pendingUpdatesRef.current = {};
      updateTask.mutate({ taskId, ...finalUpdates });
    }
  }, [taskId, updateTask]);

  // Flush pending updates when the component unmounts OR when taskId changes!
  useEffect(() => {
    return () => {
      flushPendingUpdates();
    };
  }, [flushPendingUpdates]);

  const updateField = useCallback(
    (updates: Omit<UpdateTaskRequest, "taskId">) => {
      if (!taskId) return;

      // 1. Instantly write to the React Query cache so the UI updates with 0ms perceived lag
      queryClient.setQueryData(
        [...workspaceKeys.all, "task", taskId],
        (old: any) => {
          if (!old) return old;
          return {
            ...old,
            ...updates
          };
        }
      );

      // 2. Coalesce/merge the updates
      pendingUpdatesRef.current = {
        ...pendingUpdatesRef.current,
        ...updates
      };

      // 3. Debounce the mutation trigger to prevent network spam
      if (timerRef.current) {
        clearTimeout(timerRef.current);
      }

      timerRef.current = setTimeout(() => {
        const finalUpdates = pendingUpdatesRef.current;
        pendingUpdatesRef.current = {};
        updateTask.mutate({ taskId, ...finalUpdates });
      }, 1000); 
    },
    [taskId, queryClient, updateTask]
  );

  if (isLoading && !task) {
    return (
      <div className="flex-1 flex items-center justify-center text-[10px] font-bold text-muted-foreground/60 uppercase tracking-widest bg-background h-full">
        Loading task...
      </div>
    );
  }
  
  if (isError) {
    return (
      <div className="flex-1 flex items-center justify-center text-[10px] font-bold text-destructive/80 uppercase tracking-widest bg-background h-full">
        Failed to load task
      </div>
    );
  }
  
  if (!task) return null;

  return (
    <TaskEditorContext.Provider value={{ task, updateField, isSaving }}>
      {children}
    </TaskEditorContext.Provider>
  );
}

export function useTaskEditor() {
  const context = useContext(TaskEditorContext);
  if (!context) {
    throw new Error("useTaskEditor must be used within a TaskEditorProvider");
  }
  return context;
}
