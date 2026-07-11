import type { WorkspaceRootStore } from '@/stores/workspace-root.store'
import type { Status } from '@/types/status'
import { RowAction } from '@/types/row-action'
import { api } from '@/lib/api-client'
import { toJS } from 'mobx'

export interface StatusUpdateValue {
  id: string | null
  name: string
  color: string
  orderKey: string | null
  spaceId?: string | null
  action: RowAction
}

export class StatusMutations {
  private rootStore: WorkspaceRootStore

  constructor(rootStore: WorkspaceRootStore) {
    this.rootStore = rootStore
  }

  async updateBatch(statuses: StatusUpdateValue[]): Promise<void> {
    const store = this.rootStore.statusStore
    const db = this.rootStore.statusDB!
    const previous = store.all.map((s) => toJS(s))

    // 1. Optimistic — apply every row's local effect before sending
    for (const dto of statuses) {
      if (dto.action === RowAction.Delete) {
        if (!dto.id) continue
        store.remove(dto.id)
        await db.delete(dto.id)
        continue
      }

      if (!dto.id) continue // Create/Update always carry a client-generated id
      const record: Status = {
        id: dto.id,
        spaceId: dto.spaceId ?? undefined,
        name: dto.name,
        color: dto.color,
        orderKey: dto.orderKey ?? '',
      }
      store.upsert(record)
      await db.put(record)
    }

    try {
      await api.put(
        '/statuses/sync/batch',
        { statuses },
        {
          headers: {
            'X-Workspace-Id': this.rootStore.workspaceId,
            'X-Client-Trace-Id': crypto.randomUUID(),
          },
        },
      )
    } catch (err) {
      // 2. Rollback — restore exactly what was there before, drop anything newly created
      const previousIds = new Set(previous.map((s) => s.id))
      for (const dto of statuses) {
        if (dto.id && (dto.action === RowAction.Create || dto.action === RowAction.Update) && !previousIds.has(dto.id)) {
          store.remove(dto.id)
          await db.delete(dto.id)
        }
      }
      for (const s of previous) {
        store.upsert(s)
        await db.put(s)
      }
      throw err
    }
  }
}
