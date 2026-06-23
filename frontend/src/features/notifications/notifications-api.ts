import { workspaceApi } from "@/store/workspaceApi";
import { notificationSlice } from "@/store/entityStore";
import type { NotificationRecord } from "@/types/notification-record";

interface GetNotificationsResponse {
  items: NotificationRecord[];
  nextCursor: string | null;
  hasNextPage: boolean;
  unreadCount: number;
}

export const notificationsApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getNotifications: build.query<GetNotificationsResponse, { cursor?: string | null; unreadOnly?: boolean }>({
      query: ({ cursor, unreadOnly }) => ({
        url: "/notifications",
        method: "GET",
        params: { cursor, unreadOnly },
      }),
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled;
          dispatch(notificationSlice.actions.upsertMany(data.items));
        } catch (error) {
          console.error("[notificationsApi] Failed to load notifications:", error);
        }
      },
    }),

    markNotificationsRead: build.mutation<void, { ids?: string[] }>({
      query: ({ ids }) => ({
        url: "/notifications/read",
        method: "PUT",
        data: { ids: ids ?? null },
      }),
      async onQueryStarted({ ids }, { dispatch, queryFulfilled }) {
        if (ids?.length) {
          dispatch(notificationSlice.actions.markRead(ids));
        } else {
          dispatch(notificationSlice.actions.markAllRead());
        }
        try {
          await queryFulfilled;
        } catch {
          // No rollback needed — read state is not critical
        }
      },
    }),
  }),
});

export const { useGetNotificationsQuery, useMarkNotificationsReadMutation } = notificationsApi;
