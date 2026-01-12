import SettingsIndex from '@/features/workspace/contents/setting/settings-index'
import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/workspace/$id/settings')({
  component: SettingsIndex,
})
