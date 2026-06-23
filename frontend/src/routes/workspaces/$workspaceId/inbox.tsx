import { createFileRoute } from '@tanstack/react-router'
import { InboxView } from '@/features/workspace/contents/inbox/inbox-view'

export const Route = createFileRoute('/workspaces/$workspaceId/inbox')({
  component: InboxView,
})
