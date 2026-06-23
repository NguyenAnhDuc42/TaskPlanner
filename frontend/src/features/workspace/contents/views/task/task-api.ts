import { workspaceApi } from "@/store/workspaceApi";
import { taskSlice, assigneeSlice, commentSlice } from "@/store/entityStore";
import type { TaskRecord } from "@/types/projects/task-record";
import type { CommentRecord, AssigneeRecord } from "@/types/projects";
import type { RootState } from "@/store";
import type { PagedResult } from "@/types/paged-result";
import { toast } from "sonner";
import { extractErrorMessage } from "@/types/api-error";
import { useState, useCallback, useRef, useEffect, useLayoutEffect } from "react";
import { store } from "@/store";

export type UpdateTaskPayload = Partial<TaskRecord> & {
  clearStartDate?: boolean;
  clearDueDate?: boolean;
};

export const taskApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getTaskDetail: build.query<TaskRecord[], string>({
      query: (taskId) => ({
        url: `/tasks/${taskId}`,
        method: "GET",
      }),
      providesTags: (_result, _error, id) => [{ type: "Tasks" as const, id }],
      async onQueryStarted(taskId, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(taskSlice.actions.upsertMany(data));
        } catch (error) {
          console.error(`[taskApi] Failed to fetch details for task ${taskId}:`, error);
        }
      },
    }),

    updateTask: build.mutation<void, { taskId: string; patches: UpdateTaskPayload }>({
      query: ({ taskId, patches }) => ({
        url: `/tasks/${taskId}`,
        method: "PUT",
        data: patches,
      }),
      async onQueryStarted({ taskId, patches }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        const originalTask = state.tasks.entities[taskId];
        const optimisticPatches = {
          ...patches,
          ...(patches.clearStartDate ? { startDate: null } : {}),
          ...(patches.clearDueDate ? { dueDate: null } : {}),
        };
        dispatch(taskSlice.actions.upsert({ id: taskId, ...optimisticPatches }));
        try {
          await queryFulfilled;
        } catch (err) {
          if (originalTask) {
            dispatch(taskSlice.actions.upsert(originalTask));
          }
          toast.error(extractErrorMessage(err, "Failed to update task. Your changes have been reverted."));
        }
      }
    }),

    getTaskAssignees: build.query<AssigneeRecord[], string>({
      query: (taskId) => ({
        url: `/tasks/${taskId}/assignees`,
        method: "GET",
      }),
      providesTags: (_result, _error, id) => [{ type: "Tasks" as const, id: `assignees-${id}` }],
      async onQueryStarted(taskId, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          const mappedAssignees = data.map(a => ({
            id: a.id,
            taskId: a.taskId,
            workspaceMemberId: a.workspaceMemberId
          }));
          dispatch(assigneeSlice.actions.upsertMany(mappedAssignees));
        } catch (error) {
          console.error(`[taskApi] Failed to fetch assignees for task ${taskId}:`, error);
        }
      },
    }),

    updateTaskAssignees: build.mutation<void, { taskId: string; changes: { id?: string; memberId: string; isDelete: boolean }[] }>({
      query: ({ taskId, changes }) => ({
        url: `/tasks/${taskId}/assignees`,
        method: "PUT",
        data: changes,
      }),
      invalidatesTags: (_result, _error, { taskId }) => [{ type: "Tasks" as const, id: `assignees-${taskId}` }],
      async onQueryStarted({ taskId, changes }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        const allAssignees: AssigneeRecord[] = Object.values(state.assignees.entities) as AssigneeRecord[];
        const originalAssignees = allAssignees.filter(a => a?.taskId === taskId);

        // Optimistically apply modifications to the store
        changes.forEach(c => {
          if (c.isDelete) {
            if (c.id) {
              dispatch(assigneeSlice.actions.remove(c.id));
            } else {
              const found = originalAssignees.find(a => a.workspaceMemberId === c.memberId);
              if (found) {
                dispatch(assigneeSlice.actions.remove(found.id));
              }
            }
          } else if (c.id) {
            dispatch(assigneeSlice.actions.upsert({ id: c.id, taskId, workspaceMemberId: c.memberId }));
          }
        });

        try {
          await queryFulfilled;
        } catch (err) {
          const updatedAllAssignees: AssigneeRecord[] = Object.values((getState() as RootState).assignees.entities) as AssigneeRecord[];
          const currentAssignees = updatedAllAssignees.filter(a => a?.taskId === taskId);
          dispatch(assigneeSlice.actions.removeMany(currentAssignees.map(a => a.id)));
          dispatch(assigneeSlice.actions.upsertMany(originalAssignees));
          toast.error(extractErrorMessage(err, "Failed to update task assignees. Your changes have been reverted."));
        }
      }
    }),

    getTaskComments: build.query<PagedResult<CommentRecord>, { taskId: string; cursor?: string | null }>({
      query: ({ taskId, cursor }) => ({
        url: `/tasks/${taskId}/comments`,
        method: "GET",
        params: cursor ? { cursor } : undefined,
      }),
      serializeQueryArgs: ({ queryArgs }) => `getTaskComments_${queryArgs.taskId}`,
      merge: (cache, newPage, { arg }) => {
        if (!arg.cursor) {
          cache.items = newPage.items;
        } else {
          cache.items.push(...newPage.items);
        }
        cache.nextCursor = newPage.nextCursor;
        cache.hasNextPage = newPage.hasNextPage;
      },
      forceRefetch: ({ currentArg, previousArg }) => currentArg?.cursor !== previousArg?.cursor,
      async onQueryStarted({ taskId }, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(commentSlice.actions.upsertMany(data.items));
        } catch (error) {
          console.error(`[taskApi] Failed to fetch comments for task ${taskId}:`, error);
        }
      },
    }),

    addComment: build.mutation<CommentRecord, { taskId: string; content: string; parentCommentId?: string }>({
      query: ({ taskId, content, parentCommentId }) => ({
        url: `/tasks/${taskId}/comments`,
        method: "POST",
        data: { content, parentCommentId },
      }),
      async onQueryStarted({ taskId }, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(commentSlice.actions.upsert(data));
          // Reset cursor so the merged cache shows the new comment
          dispatch(taskApi.util.updateQueryData("getTaskComments", { taskId }, (draft) => {
            if (!draft.items.some((c) => c.id === data.id)) {
              draft.items.push(data);
            }
          }));
        } catch (error) {
          console.error("[taskApi] Failed to sync new comment to cache:", error);
        }
      },
    }),

    deleteComment: build.mutation<void, { taskId: string; commentId: string }>({
      query: ({ taskId, commentId }) => ({
        url: `/tasks/${taskId}/comments/${commentId}`,
        method: "DELETE",
      }),
      async onQueryStarted({ taskId, commentId }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        const originalComment = state.comments.entities[commentId];
        dispatch(commentSlice.actions.remove(commentId));
        dispatch(taskApi.util.updateQueryData("getTaskComments", { taskId }, (draft) => {
          draft.items = draft.items.filter((c) => c.id !== commentId);
        }));
        try {
          await queryFulfilled;
        } catch (err) {
          if (originalComment) {
            dispatch(commentSlice.actions.upsert(originalComment));
          }
          toast.error(extractErrorMessage(err, "Failed to delete comment."));
        }
      }
    }),



    createSubTask: build.mutation<void, { parentTaskId: string; name: string; priority: string; statusId?: string }>({
      query: ({ parentTaskId, name, priority, statusId }) => ({
        url: `/tasks/${parentTaskId}/subtasks`,
        method: "POST",
        data: { name, priority, statusId },
      }),
      invalidatesTags: (_result, _error, { parentTaskId }) => [
        { type: "Tasks" as const, id: parentTaskId },
        { type: "Tasks" as const, id: `subtasks-${parentTaskId}` }
      ],
    }),
  }),
});

export const {
  useGetTaskDetailQuery,
  useUpdateTaskMutation,
  useGetTaskAssigneesQuery,
  useUpdateTaskAssigneesMutation,
  useGetTaskCommentsQuery,
  useAddCommentMutation,
  useDeleteCommentMutation,
  useCreateSubTaskMutation,
} = taskApi;

export function useDebouncedTaskUpdate(taskId: string, delay = 2000) {
  const [updateTask] = useUpdateTaskMutation();
  const pendingRef = useRef<UpdateTaskPayload>({});
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const updateTaskRef = useRef(updateTask);
  useLayoutEffect(() => { updateTaskRef.current = updateTask; });

  // Flush on unmount OR when taskId changes (user switches to a different task)
  useEffect(() => {
    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
      const patches = { ...pendingRef.current };
      pendingRef.current = {};
      // taskId is captured from this render's closure — always the task being left
      if (Object.keys(patches).length > 0) updateTaskRef.current({ taskId, patches });
    };
  }, [taskId]);

  return useCallback((patches: UpdateTaskPayload) => {
    pendingRef.current = { ...pendingRef.current, ...patches };

    // Optimistic update immediately
    store.dispatch(taskSlice.actions.upsert({
      id: taskId,
      ...patches,
      ...(patches.clearStartDate ? { startDate: null } : {}),
      ...(patches.clearDueDate ? { dueDate: null } : {}),
    }));

    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => {
      const toSend = { ...pendingRef.current };
      pendingRef.current = {};
      if (Object.keys(toSend).length > 0) updateTaskRef.current({ taskId, patches: toSend });
    }, delay);
  }, [taskId, delay]);
}

export function useTaskComments(taskId: string) {
  const [cursor, setCursor] = useState<string | null>(null);

  const { data, isLoading, isFetching } = useGetTaskCommentsQuery(
    { taskId, cursor },
    { skip: !taskId }
  );

  const fetchNextPage = useCallback(() => {
    if (data) setCursor(data.nextCursor ?? null);
  }, [data]);

  return {
    isLoading,
    isFetchingNextPage: isFetching && cursor !== null,
    hasNextPage: !!data?.hasNextPage,
    fetchNextPage,
  };
}
