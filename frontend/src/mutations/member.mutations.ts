import type { WorkspaceRootStore } from '@/stores/workspace-root.store'
import type { MemberRecord } from '@/types/workspace/member-record'
import type { Role } from '@/types/role'
import type { MembershipStatus } from '@/types/membership-status'
import { api } from '@/lib/api-client'
import { toJS } from 'mobx'

// Members are workspace-wide (not per-member-personal like Favorites), and Update/Remove DO
// broadcast via SyncEvent/Delta to the rest of the workspace — but this still can't go through
// TransactionQueue like Task/Folder/Space: there's no BatchFlushHandler case for Member, so a
// queued Member mutation would never actually get replayed offline. Backend-first instead, same
// shape as Workspace/Favorite/Status mutations: apply optimistically, one call, roll back on
// failure. Add is the odd one out even among these — see add()'s comment.
export class MemberMutations {
  private rootStore: WorkspaceRootStore

  constructor(rootStore: WorkspaceRootStore) {
    this.rootStore = rootStore
  }

  private headers() {
    return {
      'X-Workspace-Id': this.rootStore.workspaceId,
      'X-Client-Trace-Id': crypto.randomUUID(),
    }
  }

  // No optimistic step here — unlike Task/Space/Folder, the client can't generate a member's id
  // up front (it depends on resolving an email to an existing user server-side), so there's
  // nothing valid to write to the store before the round trip. The server also can't broadcast a
  // Delta back to the adding client's own connection (GroupExcept), so the response body is the
  // only way this client ever learns about the members it just added — see AddMembersResult.
  async add(members: { email: string; role: Role }[]): Promise<void> {
    const { data } = await api.post<{ syncEventId: number; members: MemberRecord[] }>(
      '/members/sync/add',
      { members },
      { headers: this.headers() },
    )
    for (const record of data.members) {
      this.rootStore.memberStore.upsert(record)
      await this.rootStore.memberDB!.put(record)
    }
  }

  async update(updates: { memberId: string; role?: Role; status?: MembershipStatus }[]): Promise<void> {
    const store = this.rootStore.memberStore
    const previous = updates
      .map((u) => store.getById(u.memberId))
      .filter((m): m is MemberRecord => !!m)
      .map((m) => toJS(m))

    // 1. Optimistic — only include keys actually being changed, so an undefined role/status on
    // one row doesn't spread-overwrite the other field with undefined.
    for (const u of updates) {
      const patch: Partial<MemberRecord> = {}
      if (u.role !== undefined) patch.role = u.role
      if (u.status !== undefined) patch.status = u.status
      store.update(u.memberId, patch)
      const record = store.getById(u.memberId)
      if (record) await this.rootStore.memberDB!.put(toJS(record))
    }

    try {
      await api.put('/members/sync/batch', { members: updates }, { headers: this.headers() })
    } catch (err) {
      // 2. Rollback
      for (const record of previous) {
        store.upsert(record)
        await this.rootStore.memberDB!.put(record)
      }
      throw err
    }
  }

  async remove(memberIds: string[]): Promise<void> {
    const store = this.rootStore.memberStore
    const previous = memberIds
      .map((id) => store.getById(id))
      .filter((m): m is MemberRecord => !!m)
      .map((m) => toJS(m))

    // 1. Optimistic
    for (const id of memberIds) {
      store.remove(id)
      await this.rootStore.memberDB!.delete(id)
    }

    try {
      await api.post('/members/sync/remove', { memberIds }, { headers: this.headers() })
    } catch (err) {
      // 2. Rollback
      for (const record of previous) {
        store.upsert(record)
        await this.rootStore.memberDB!.put(record)
      }
      throw err
    }
  }
}
