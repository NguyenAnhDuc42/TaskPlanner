import type { RootStore } from '@/stores/root.store'
import type { Status } from '@/types/status'
import type { StatusCategory } from '@/types/status-category'
import { RowAction } from '@/types/row-action'
import { api } from '@/lib/api-client'
import { toJS } from 'mobx'

export interface StatusUpdateValue {
  id: string | null
  name: string
  color: string
  category: StatusCategory
  orderKey: string | null
  action: RowAction
}

// No BatchFlushHandler counterpart exists for Status on the backend — unlike Task/Folder/Space,
// a Status batch can't be queued and replayed offline through TransactionQueue. This is a single
// backend-first "save the whole batch or roll it all back" call, same shape as Workspace/Favorite
// mutations: apply optimistically, one PUT to /statuses/sync/batch, full rollback on any failure
// (network or rejection alike — there's nowhere to leave a "pending" batch waiting for retry).
export class StatusMutations {
  private rootStore: RootStore

  constructor(rootStore: RootStore) {
    this.rootStore = rootStore
  }

  async updateBatch(spaceId: string, statuses: StatusUpdateValue[]): Promise<void> {
    const store = this.rootStore.statusStore
    const db = this.rootStore.statusDB!
    const previous = store.getBySpace(spaceId).map((s) => toJS(s))

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
        spaceId,
        name: dto.name,
        color: dto.color,
        category: dto.category,
        orderKey: dto.orderKey ?? '',
      }
      store.upsert(record)
      await db.put(record)
    }

    try {
      await api.put(
        '/statuses/sync/batch',
        { spaceId, statuses },
        {
          headers: {
            'X-Workspace-Id': this.rootStore.currentWorkspaceId!,
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
