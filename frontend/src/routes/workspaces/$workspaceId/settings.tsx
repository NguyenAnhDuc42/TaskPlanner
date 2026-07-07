import { createFileRoute } from '@tanstack/react-router'
import { ComingSoon } from '@/components/coming-soon'
import { LoadingScreen } from '@/components/loading-screen'

export const Route = createFileRoute('/workspaces/$workspaceId/settings')({
  pendingComponent: LoadingScreen,
  component: () => (
    <ComingSoon 
      title="Settings" 
      description="We're building a comprehensive settings hub to manage your workspace preferences, members, and custom workflows." 
    />
  ),
})
