import type { RootStore } from '@/stores/root.store'
import { EntityLayerType } from '@/types/entity-layer-type'
import { api } from '@/lib/api-client'
import { fractionalAfter } from '@/features/workspace/contents/hierarchy/utils/fractional-index'

// Favorites are deliberately NOT part of the sync engine — they're personal (per
// WorkspaceMember), and the SyncHub group broadcasts to the whole workspace, so a Task/Comment-
// style SyncEvent would leak "who favorited what" to every other member (see
// ToggleFavoriteCommand.cs). That means no Bootstrap-after-first-load coverage and no Delta ever
// corrects this client-side, so — unlike every other *Mutations class — this talks to its own
// REST endpoints directly and patches the MobX store optimistically itself, with manual rollback
// on failure, instead of going through TransactionQueue.
export class FavoriteMutations {
  private rootStore: RootStore

  constructor(rootStore: RootStore) {
    this.rootStore = rootStore
  }

  private storeFor(entityLayerType: EntityLayerType) {
    if (entityLayerType === EntityLayerType.ProjectSpace) return this.rootStore.spaceStore
    if (entityLayerType === EntityLayerType.ProjectFolder) return this.rootStore.folderStore
    return this.rootStore.taskStore
  }

  // OrderKey is only used server-side when this toggle ADDS a favorite (ignored on remove) —
  // computed here as "after the last favorite across all three entity types", since the
  // favorites list in the sidebar is one mixed-type ordering, not per-entity-type.
  private nextFavoriteOrderKey(): string {
    const allKeys = [
      ...this.rootStore.spaceStore.getFavorites(),
      ...this.rootStore.folderStore.getFavorites(),
      ...this.rootStore.taskStore.getFavorites(),
    ].map((f) => f.favoriteOrderKey).filter((k): k is string => !!k)
    const max = allKeys.sort().at(-1)
    return fractionalAfter(max)
  }

  async toggle(entityId: string, entityLayerType: EntityLayerType): Promise<void> {
    const store = this.storeFor(entityLayerType)
    const previous = store.getById(entityId)
    if (!previous) return
    const wasFavorite = !!previous.isFavorite
    const previousOrderKey = previous.favoriteOrderKey
    const orderKey = this.nextFavoriteOrderKey()

    // 1. Optimistic flip
    store.update(entityId, { isFavorite: !wasFavorite })

    try {
      const { data } = await api.post<{ isFavorite: boolean; favoriteOrderKey: string | null }>(
        `/workspaces/${this.rootStore.currentWorkspaceId}/favorites/toggle`,
        { entityId, entityLayerType, orderKey },
      )
      // 2. Confirm with server's authoritative state
      store.update(entityId, { isFavorite: data.isFavorite, favoriteOrderKey: data.favoriteOrderKey ?? undefined })
    } catch (err) {
      // 3. Rollback
      store.update(entityId, { isFavorite: wasFavorite, favoriteOrderKey: previousOrderKey })
      throw err
    }
  }

  async reorder(entityId: string, entityLayerType: EntityLayerType, previousOrderKey: string | null, nextOrderKey: string | null, newOrderKey: string): Promise<void> {
    const store = this.storeFor(entityLayerType)
    const previous = store.getById(entityId)
    if (!previous) return
    const previousFavoriteOrderKey = previous.favoriteOrderKey

    // 1. Optimistic
    store.update(entityId, { favoriteOrderKey: newOrderKey })

    try {
      await api.put(`/workspaces/${this.rootStore.currentWorkspaceId}/favorites/reorder`, {
        entityId, entityLayerType, previousOrderKey, nextOrderKey,
      })
    } catch (err) {
      // 2. Rollback
      store.update(entityId, { favoriteOrderKey: previousFavoriteOrderKey })
      throw err
    }
  }
}
