import { workspaceApi } from "@/store/workspaceApi";
import { taskSlice, assigneeSlice } from "@/store/entityStore";
import type { TaskRecord } from "@/types/projects/task-record";
import type { CommentRecord, AssigneeRecord } from "@/types/projects";

export const taskApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getTaskDetail: build.query<TaskRecord, string>({
      query: (taskId) => ({
        url: `/tasks/${taskId}`,
        method: "GET",
      }),
      providesTags: (_result, _error, id) => [{ type: "Tasks" as const, id }],
      async onQueryStarted(_taskId, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(taskSlice.actions.upsert(data));
        } catch {}
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
        const state = getState() as any;
        const originalTask = state.tasks.entities[taskId];
        dispatch(taskSlice.actions.upsert({ id: taskId, ...patches }));
        try {
          await queryFulfilled;
        } catch {
          if (originalTask) {
            dispatch(taskSlice.actions.upsert(originalTask));
          }
        }
      }
    }),

    getTaskAssignees: build.query<AssigneeRecord[], string>({
      query: (taskId) => ({
        url: `/tasks/${taskId}/assignees`,
        method: "GET",
      }),
      providesTags: (_result, _error, id) => [{ type: "Tasks" as const, id: `assignees-${id}` }],
      async onQueryStarted(_taskId, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          const mappedAssignees = data.map(a => ({
            id: a.id,
            taskId: a.taskId,
            workspaceMemberId: a.workspaceMemberId
          }));
          dispatch(assigneeSlice.actions.upsertMany(mappedAssignees));
        } catch {}
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
        const state = getState() as any;
        const allAssignees: AssigneeRecord[] = Object.values(state.assignees.entities);
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
          const updatedAllAssignees: AssigneeRecord[] = Object.values((getState() as any).assignees.entities);
          const currentAssignees = updatedAllAssignees.filter(a => a && a.taskId === taskId);
          dispatch(assigneeSlice.actions.removeMany(currentAssignees.map(a => a.id)));
          dispatch(assigneeSlice.actions.upsertMany(originalAssignees));
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
  }),
});

export const {
  useGetTaskDetailQuery,
  useUpdateTaskMutation,
  useGetTaskAssigneesQuery,
  useUpdateTaskAssigneesMutation,
  useGetTaskCommentsQuery,
  useAddCommentMutation,
} = taskApi;
