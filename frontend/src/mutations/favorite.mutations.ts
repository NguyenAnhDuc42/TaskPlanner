import type { WorkspaceRootStore } from '@/stores/workspace-root.store'
import type { FavoriteRecord } from '@/types/projects/favorite-record'
import { EntityLayerType } from '@/types/entity-layer-type'
import { api } from '@/lib/api-client'
import { fractionalAfter } from '@/features/workspace/contents/hierarchy/utils/fractional-index'
import { toJS } from 'mobx'

// Favorites are deliberately NOT part of the sync engine — they're personal (per
// WorkspaceMember), and the SyncHub group broadcasts to the whole workspace, so a Task/Comment-
// style SyncEvent would leak "who favorited what" to every other member (see
// ToggleFavoriteCommand.cs). That means no Delta ever corrects this client-side, so — unlike
// every other *Mutations class — this talks to its own REST endpoints directly and patches its
// own store optimistically, with manual rollback on failure, instead of going through
// TransactionQueue.
//
// A favorite lives in its own FavoriteStore/FavoriteDB, keyed by entityId — not as fields on
// TaskRecord/FolderRecord/SpaceRecord. Putting it there used to mean every unrelated Task/Folder/
// Space update (via Delta, from anyone) silently wiped favorite state, since those payloads never
// carry isFavorite/favoriteOrderKey. Keeping it as its own record means nothing about the sync
// engine's Task/Folder/Space path can touch it at all.
export class FavoriteMutations {
  private rootStore: WorkspaceRootStore

  constructor(rootStore: WorkspaceRootStore) {
    this.rootStore = rootStore
  }

  private async persist(record: FavoriteRecord): Promise<void> {
    await this.rootStore.favoriteDB!.put(toJS(record))
  }

  private async unpersist(entityId: string): Promise<void> {
    await this.rootStore.favoriteDB!.delete(entityId)
  }

  private nextFavoriteOrderKey(): string {
    const max = this.rootStore.favoriteStore.all
      .map((f) => f.orderKey)
      .filter((k): k is string => !!k)
      .sort()
      .at(-1)
    return fractionalAfter(max)
  }

  async toggle(entityId: string, entityLayerType: EntityLayerType): Promise<void> {
    const store = this.rootStore.favoriteStore
    const previous = store.getByEntityId(entityId)
    const wasFavorite = !!previous
    const orderKey = this.nextFavoriteOrderKey()

    // 1. Optimistic — id is a client-side placeholder; the toggle response doesn't echo back the
    // server's real favorite row id, and nothing on the frontend keys off it (the store keys by
    // entityId), so it's never load-bearing.
    const optimistic: FavoriteRecord = { id: crypto.randomUUID(), entityId, entityLayerType, orderKey }
    if (wasFavorite) {
      store.remove(entityId)
      await this.unpersist(entityId)
    } else {
      store.upsert(optimistic)
      await this.persist(optimistic)
    }

    try {
      const { data } = await api.post<{ isFavorite: boolean; favoriteOrderKey: string | null }>(
        `/favorites/toggle`,
        { entityId, entityLayerType, orderKey },
        { headers: { 'X-Client-Trace-Id': crypto.randomUUID() } },
      )
      // 2. Confirm with server's authoritative state
      if (data.isFavorite) {
        const confirmed: FavoriteRecord = { ...optimistic, orderKey: data.favoriteOrderKey ?? orderKey }
        store.upsert(confirmed)
        await this.persist(confirmed)
      } else {
        store.remove(entityId)
        await this.unpersist(entityId)
      }
    } catch (err) {
      // 3. Rollback
      if (previous) {
        store.upsert(previous)
        await this.persist(previous)
      } else {
        store.remove(entityId)
        await this.unpersist(entityId)
      }
      throw err
    }
  }

  async reorder(entityId: string, entityLayerType: EntityLayerType, previousOrderKey: string | null, nextOrderKey: string | null, newOrderKey: string): Promise<void> {
    const store = this.rootStore.favoriteStore
    const previous = store.getByEntityId(entityId)
    if (!previous) return

    // 1. Optimistic
    const updated: FavoriteRecord = { ...previous, orderKey: newOrderKey }
    store.upsert(updated)
    await this.persist(updated)

    try {
      await api.put(
        `/favorites/reorder`,
        { entityId, entityLayerType, previousOrderKey, nextOrderKey, orderKey: newOrderKey },
        { headers: { 'X-Client-Trace-Id': crypto.randomUUID() } },
      )
    } catch (err) {
      // 2. Rollback
      store.upsert(previous)
      await this.persist(previous)
      throw err
    }
  }
}
