import { workspaceApi } from "@/store/workspaceApi";
import { taskSlice, assigneeSlice } from "@/store/entityStore";
import type { TaskRecord } from "@/types/projects/task-record";
import type { CommentRecord, AssigneeRecord } from "@/types/projects";
import type { RootState } from "@/store";
import { toast } from "sonner";

export const taskApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getTaskDetail: build.query<TaskRecord[], string>({
      query: (taskId) => ({
        url: `/tasks/${taskId}`,
        method: "GET",
      }),
      providesTags: (_result, _error, id) => [{ type: "Tasks" as const, id }],
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(taskSlice.actions.upsertMany(data));
        } catch { /* ignore */ }
      },
    }),

    updateTask: build.mutation<void, { taskId: string; patches: Partial<TaskRecord> }>({
      query: ({ taskId, patches }) => ({
        url: `/tasks/${taskId}`,
        method: "PUT",
        data: patches,
      }),
      invalidatesTags: (_result, _error, { taskId }) => [{ type: "Tasks" as const, id: taskId }],
      async onQueryStarted({ taskId, patches }, { dispatch, queryFulfilled, getState }) {
        const state = getState() as RootState;
        const originalTask = state.tasks.entities[taskId];
        dispatch(taskSlice.actions.upsert({ id: taskId, ...patches }));
        try {
          await queryFulfilled;
        } catch {
          if (originalTask) {
            dispatch(taskSlice.actions.upsert(originalTask));
          }
          toast.error("Failed to update task. Your changes have been reverted.");
        }
      }
    }),

    getTaskAssignees: build.query<AssigneeRecord[], string>({
      query: (taskId) => ({
        url: `/tasks/${taskId}/assignees`,
        method: "GET",
      }),
      providesTags: (_result, _error, id) => [{ type: "Tasks" as const, id: `assignees-${id}` }],
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          const mappedAssignees = data.map(a => ({
            id: a.id,
            taskId: a.taskId,
            workspaceMemberId: a.workspaceMemberId
          }));
          dispatch(assigneeSlice.actions.upsertMany(mappedAssignees));
        } catch { /* ignore */ }
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
        const originalAssignees = allAssignees.filter(a => a && a.taskId === taskId);

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
          } else {
            if (c.id) {
              dispatch(assigneeSlice.actions.upsert({ id: c.id, taskId, workspaceMemberId: c.memberId }));
            }
          }
        });

        try {
          await queryFulfilled;
        } catch {
          // Rollback on failure
          const updatedAllAssignees: AssigneeRecord[] = Object.values((getState() as RootState).assignees.entities) as AssigneeRecord[];
          const currentAssignees = updatedAllAssignees.filter(a => a && a.taskId === taskId);
          dispatch(assigneeSlice.actions.removeMany(currentAssignees.map(a => a.id)));
          dispatch(assigneeSlice.actions.upsertMany(originalAssignees));
          toast.error("Failed to update task assignees. Your changes have been reverted.");
        }
      }
    }),

    getTaskComments: build.query<CommentRecord[], string>({
      query: (taskId) => ({
        url: `/tasks/${taskId}/comments`,
        method: "GET",
      }),
      providesTags: (_result, _error, id) => [{ type: "Tasks" as const, id: `comments-${id}` }],
    }),

    addComment: build.mutation<CommentRecord, { taskId: string; content: string; parentCommentId?: string }>({
      query: ({ taskId, content, parentCommentId }) => ({
        url: `/tasks/${taskId}/comments`,
        method: "POST",
        data: { content, parentCommentId },
      }),
      invalidatesTags: (_result, _error, { taskId }) => [{ type: "Tasks" as const, id: `comments-${taskId}` }],
    }),

    createSubTask: build.mutation<void, { spaceId: string; parentTaskId: string; name: string; priority: string }>({
      query: ({ spaceId, parentTaskId, name, priority }) => ({
        url: `/tasks/${parentTaskId}/subtasks`,
        method: "POST",
        data: { spaceId, name, priority },
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
  useCreateSubTaskMutation,
} = taskApi;
