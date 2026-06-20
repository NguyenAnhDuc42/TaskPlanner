import { createFileRoute } from '@tanstack/react-router'
import { ComingSoon } from '@/components/coming-soon'

export const Route = createFileRoute('/workspaces/$workspaceId/settings')({
  component: () => (
    <ComingSoon 
      title="Settings" 
      description="We're building a comprehensive settings hub to manage your workspace preferences, members, and custom workflows." 
    />
  ),
})
