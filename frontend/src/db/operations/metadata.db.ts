import type { IDBPDatabase } from 'idb'
import type { TaskPlanDB } from '../schema'
import type { WorkspaceMetadata } from '@/types/sync'


const META_KEY = 'meta'

export class MetadataDB {
  private db: IDBPDatabase<TaskPlanDB>
  private workspaceId:string
  constructor(db: IDBPDatabase<TaskPlanDB>,workspaceId: string) {
    this.db = db
    this.workspaceId = workspaceId
  }

  async get(): Promise<WorkspaceMetadata | undefined> {
    return this.db.get('__metadata', META_KEY)
  }
 
  async getLastSyncId(): Promise<number> {
    const meta = await this.get()
    return meta?.lastSyncId ?? 0
  }
 
  async setLastSyncId(syncId: number): Promise<void> {
    const existing = await this.get()
    await this.db.put(
      '__metadata',
      {
        workspaceId: this.workspaceId,
        lastSyncId: syncId,
        firstSyncId: existing?.firstSyncId ?? syncId,
        databaseVersion: existing?.databaseVersion ?? 1,
        bootstrappedAt: Date.now(),
      },
      META_KEY
    )
  }
 
   async setFullBootstrap(syncId: number, dbVersion: number): Promise<void> {
    await this.db.put(
      '__metadata',
      {
        workspaceId: this.workspaceId,
        lastSyncId: syncId,
        firstSyncId: syncId,
        databaseVersion: dbVersion,
        bootstrappedAt: Date.now(),
      },
      META_KEY
    )
  }
}