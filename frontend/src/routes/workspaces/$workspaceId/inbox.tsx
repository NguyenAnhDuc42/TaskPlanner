import { createFileRoute } from '@tanstack/react-router'
import { ComingSoon } from '@/components/coming-soon'

export const Route = createFileRoute('/workspaces/$workspaceId/inbox')({
  component: () => (
    <ComingSoon 
      title="Inbox" 
      description="Your personal triage center is under construction. Soon, you'll be able to manage all your notifications and assigned tasks in one place." 
    />
  ),
})
