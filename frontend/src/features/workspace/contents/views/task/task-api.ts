import { workspaceApi } from "@/store/workspaceApi";
import { taskSlice, taskSelectors } from "@/store/entityStore";
import type { RootState } from "@/store";
import type { TaskRecord } from "@/types/projects/task-record";
import type { CommentRecord } from "@/types/projects";


export const taskApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getTaskDetail: build.query<TaskRecord, string>({
      query: (taskId) => ({
        url: `/tasks/${taskId}`,
        method: "GET",
      }),
      providesTags: (_result, _error, id) => [{ type: "Tasks" as const, id }],
      async onQueryStarted(taskId, { dispatch, queryFulfilled }) {
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
        const state = getState() as RootState;
        const originalTask = taskSelectors.selectById(state, taskId);

        // Optimistic update
        dispatch(taskSlice.actions.upsert({ id: taskId, ...patches }));

        try {
          await queryFulfilled;
        } catch {
          if (originalTask) {
            dispatch(taskSlice.actions.upsert(originalTask));
          }
        }
      },
    }),

    getComments: build.query<CommentRecord[], string>({
      query: (taskId) => ({
        url: `/tasks/${taskId}/comments`,
        method: "GET",
      }),
      providesTags: (_result, _error, taskId) => [{ type: "Tasks" as const, id: `comments-${taskId}` }],
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
  useGetCommentsQuery,
  useAddCommentMutation,
} = taskApi;
