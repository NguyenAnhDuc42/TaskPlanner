import { createFileRoute } from '@tanstack/react-router'
import { LoadingScreen } from '@/components/loading-screen'
import { WorkspaceSettingsPage } from '@/features/workspace/contents/workspace-settings/workspace-settings-page'

export const Route = createFileRoute('/workspaces/$workspaceId/settings')({
  pendingComponent: LoadingScreen,
  component: WorkspaceSettingsPage,
})
