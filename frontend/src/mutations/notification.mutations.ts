import type { RootStore } from '@/stores/root.store'
import type { NotificationRecord } from '@/types/notification-record'
import { api } from '@/lib/api-client'
import { toJS } from 'mobx'

// Notification is a read-replica entity — it bypasses Bootstrap/Delta entirely (it's scoped to
// the user across all their workspaces, not to one workspace), so it needs its own fetch instead
// of riding along with the sync engine. Real-time delivery is separate too: new notifications
// arrive over the "NewNotification" SignalR event (see use-notification-signalr.ts), not as a
// Delta. Mark-read is deliberately fire-and-forget, matching the legacy behavior it replaces —
// read state was never considered critical enough to need rollback-on-failure.
export class NotificationMutations {
  private rootStore: RootStore

  constructor(rootStore: RootStore) {
    this.rootStore = rootStore
  }

  async fetchInitial(limit = 50): Promise<void> {
    await this.fetchPage(null, limit)
  }

  // For the full Inbox view's infinite scroll — the store holds whatever's been fetched so far
  // (starts at the 50 newest from fetchInitial), the caller tracks cursor/hasNextPage locally
  // and calls this again to load further pages.
  async fetchPage(cursor: string | null, limit = 50): Promise<{ nextCursor: string | null; hasNextPage: boolean; unreadCount: number }> {
    const { data } = await api.get<{
      items: NotificationRecord[]
      nextCursor: string | null
      hasNextPage: boolean
      unreadCount: number
    }>('/notifications/sync', { params: { cursor, limit } })

    for (const record of data.items) {
      this.rootStore.notificationStore.upsert(record)
      await this.rootStore.notificationDB!.put(record)
    }

    return { nextCursor: data.nextCursor, hasNextPage: data.hasNextPage, unreadCount: data.unreadCount }
  }

  async markRead(ids?: string[]): Promise<void> {
    const store = this.rootStore.notificationStore
    const targets = ids?.length ? ids : store.all.filter((n) => !n.isRead).map((n) => n.id)

    for (const id of targets) {
      store.markRead(id)
      const record = store.getById(id)
      if (record) await this.rootStore.notificationDB!.put(toJS(record))
    }

    // No rollback on failure — read state is not critical (matches the legacy behavior).
    api.put('/notifications/sync/read', { ids: ids ?? null }).catch((err) =>
      console.error('Failed to mark notifications read', err),
    )
  }
}
