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
            id: `${a.taskId}_${a.workspaceMemberId}`,
            taskId: a.taskId,
            workspaceMemberId: a.workspaceMemberId
          }));
          dispatch(assigneeSlice.actions.upsertMany(mappedAssignees));
        } catch {}
      },
    }),

    updateTaskAssignees: build.mutation<void, { taskId: string; changes: { memberId: string; isDelete: boolean }[] }>({
      query: ({ taskId, changes }) => ({
        url: `/tasks/${taskId}/assignees`,
        method: "PUT",
        data: changes,
      }),
      invalidatesTags: (_result, _error, { taskId }) => [{ type: "Tasks" as const, id: `assignees-${taskId}` }],
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
