import { createFileRoute } from '@tanstack/react-router'
import { InboxView } from '@/features/workspace/contents/inbox/inbox-view'
import { store } from '@/store'
import { notificationsApi } from '@/features/notifications/notifications-api'
import { ViewSkeleton } from '@/components/view-skeleton'

export const Route = createFileRoute('/workspaces/$workspaceId/inbox')({
  loader: async () => {
    try {
      await store.dispatch(notificationsApi.endpoints.getNotifications.initiate({ cursor: null })).unwrap();
    } catch {
      // Non-fatal — component renders with empty/stale state
    }
  },
  pendingComponent: ViewSkeleton,
  component: InboxView,
})
