import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/workspaces/$workspaceId/communications')({
  component: RouteComponent,
})

function RouteComponent() {
  return <div>Hello "/workspace/$id/communications"!</div>
}
