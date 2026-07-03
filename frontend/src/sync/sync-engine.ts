import {
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  type HubConnection,
} from '@microsoft/signalr'
import type { RootStore } from '@/stores/root.store'
import type { DeltaPayload, DeltaBatchPayload } from '@/types/sync/delta'
import { applyDelta, applyDeltaBatch } from './delta-handler'
import { TransactionQueue } from './transaction-queue'
import type { TaskRecord, SpaceRecord, FolderRecord } from '@/types/projects'
import type { Status } from '@/types/status'
import type { PendingTransaction } from '@/types/sync'
import type { DocumentBlockRecord } from '@/types/document/document-block-record'
import type { AssigneeRecord, FavoriteRecord } from '@/types/projects'
import type { MemberRecord } from '@/types/workspace/member-record'
import { api } from '@/lib/api-client'
import { devLog } from './dev-log'

// Must track SyncQueryService.CurrentDatabaseVersion on the backend — bump both together
// whenever Bootstrap's payload shape changes. A client whose last bootstrap predates this
// gets force-rebootstrapped on init() instead of relying only on Delta catch-up, which can't
// backfill a field/entity type that didn't exist in past SyncEvents.
const EXPECTED_DATABASE_VERSION = 2

interface BootstrapResponse {
  lastSyncId: number
  databaseVersion: number
  tasks: Record<string, unknown>[]
  spaces: Record<string, unknown>[]
  folders: Record<string, unknown>[]
  statuses: Record<string, unknown>[]
  documentBlocks: Record<string, unknown>[]
  assignees: Record<string, unknown>[]
  favorites: Record<string, unknown>[]
  members: Record<string, unknown>[]
}

export class SyncEngine {
  private rootStore: RootStore
  private connection: HubConnection | null = null
  private queue: TransactionQueue

  constructor(rootStore: RootStore) {
    this.rootStore = rootStore
    this.queue = new TransactionQueue(rootStore)
  }

  get transactionQueue(): TransactionQueue {
    return this.queue
  }

  async flushQueue(): Promise<void> {
    await this.queue.flush((txs) => this.sendBatch(txs))
  }

  async forceBootstrap(workspaceId: string): Promise<void> {
    // Hard reset to server ground truth — clears local state first
    const { taskDB, spaceDB, folderDB, statusDB, documentBlockDB, assigneeDB, favoriteDB, memberDB } = this.rootStore
    await Promise.all([taskDB!.clear(), spaceDB!.clear(), folderDB!.clear(), statusDB!.clear(), documentBlockDB!.clear(), assigneeDB!.clear(), favoriteDB!.clear(), memberDB!.clear()])
    await this.bootstrap(workspaceId)
  }

  /**
   * Full initialization flow on workspace switch.
   *
   * 1. Hydrate from IndexedDB (already done in rootStore.switchWorkspace)
   * 2. Recover any interrupted transactions
   * 3. Connect SignalR (which triggers catch-up via lastSyncId)
   */
  async init(workspaceId: string): Promise<void> {
    // Disconnect previous workspace
    await this.disconnect()

    // Recover interrupted transactions from previous session
    await this.queue.recoverInFlight()

    // Check if we need full bootstrap or delta catch-up
    const meta = await this.rootStore.metadataDB!.get()
    const lastSyncId = meta?.lastSyncId ?? 0
    const isStale = (meta?.databaseVersion ?? 0) < EXPECTED_DATABASE_VERSION

    if (lastSyncId === 0 || isStale) {
      // First time, or this session's last bootstrap predates a Bootstrap shape change —
      // full bootstrap either way, since Delta catch-up alone can't backfill fields/entity
      // types that didn't exist when this session's earlier SyncEvents were recorded.
      await this.bootstrap(workspaceId)
    }

    // Connect SignalR — handshake triggers catch-up
    await this.connect(workspaceId)
  }

  // ── Bootstrap (first time only) ──

  private async bootstrap(workspaceId: string): Promise<void> {
    const res = await api.get(`/workspaces/${workspaceId}/sync/bootstrap`)
    const data: BootstrapResponse = res.data

    // Populate IndexedDB
    const { taskDB, spaceDB, folderDB, statusDB, documentBlockDB, assigneeDB, favoriteDB, memberDB, metadataDB } = this.rootStore
    await Promise.all([
      taskDB!.putMany(data.tasks as unknown as TaskRecord[]),
      spaceDB!.putMany(data.spaces as unknown as SpaceRecord[]),
      folderDB!.putMany(data.folders as unknown as FolderRecord[]),
      statusDB!.putMany(data.statuses as unknown as Status[]),
      documentBlockDB!.putMany(data.documentBlocks as unknown as DocumentBlockRecord[]),
      assigneeDB!.putMany(data.assignees as unknown as AssigneeRecord[]),
      favoriteDB!.putMany(data.favorites as unknown as FavoriteRecord[]),
      memberDB!.putMany(data.members as unknown as MemberRecord[]),
    ])

    // Set metadata
    await metadataDB!.setFullBootstrap(data.lastSyncId, data.databaseVersion)

    // Hydrate stores from what we just saved
    const [tasks, spaces, folders, statuses, documentBlocks, assignees, favorites, members] = await Promise.all([
      taskDB!.getAll(),
      spaceDB!.getAll(),
      folderDB!.getAll(),
      statusDB!.getAll(),
      documentBlockDB!.getAll(),
      assigneeDB!.getAll(),
      favoriteDB!.getAll(),
      memberDB!.getAll(),
    ])
    this.rootStore.taskStore.hydrate(tasks)
    this.rootStore.spaceStore.hydrate(spaces)
    this.rootStore.folderStore.hydrate(folders)
    this.rootStore.statusStore.hydrate(statuses)
    this.rootStore.documentBlockStore.hydrate(documentBlocks)
    this.rootStore.assigneeStore.hydrate(assignees)
    this.rootStore.favoriteStore.hydrate(favorites)
    this.rootStore.memberStore.hydrate(members)
  }

  // ── SignalR connection ──

  private async connect(workspaceId: string): Promise<void> {
    const lastSyncId = await this.rootStore.metadataDB!.getLastSyncId()

    const backendUrl = import.meta.env.VITE_API_URL ?? ''
    this.connection = new HubConnectionBuilder()
      .withUrl(
        `${backendUrl}/hubs/sync?workspaceId=${workspaceId}&lastSyncId=${lastSyncId}`,
        {
          withCredentials: true,
          transport: HttpTransportType.WebSockets,
          skipNegotiation: true,
        }
      )
      .withAutomaticReconnect({ nextRetryDelayInMilliseconds: () => 5000 })
      .build()

    // Live deltas
    this.connection.on('Delta', (delta: DeltaPayload) => {
      devLog('[SyncEngine] Delta received:', delta.entityType, delta.action, delta.syncId)
      applyDelta(this.rootStore, delta, (id) => this.queue.cancelByEntityId(id))
    })

    // Batch deltas (catch-up on connect/reconnect)
    this.connection.on('DeltaBatch', (payload: DeltaBatchPayload) => {
      devLog('[SyncEngine] DeltaBatch received:', payload.actions.length, 'events, latestSyncId:', payload.latestSyncId)
      applyDeltaBatch(this.rootStore, payload.actions, (id) => this.queue.cancelByEntityId(id))
    })

    // On reconnect, server sends catch-up automatically
    // but we also flush pending transactions
    this.connection.onreconnected(async () => {
      devLog('[SyncEngine] Reconnected — server will push DeltaBatch catch-up automatically')
      await this.queue.flush((txs) => this.sendBatch(txs))
    })

    await this.connection.start()

    // Flush any pending transactions from previous session
    await this.queue.flush((txs) => this.sendBatch(txs))
  }

  private async disconnect(): Promise<void> {
    if (
      this.connection &&
      this.connection.state !== HubConnectionState.Disconnected
    ) {
      await this.connection.stop()
      this.connection = null
    }
  }

  // ── Send mutations to server ──

  private async sendBatch(txs: PendingTransaction[]): Promise<{ traceId: string; success: boolean; error?: string | null }[]> {
    const workspaceId = this.rootStore.currentWorkspaceId
    const response = await api.post('/sync/batch', {
      items: txs.map(tx => ({
        traceId: tx.id,
        entityType: tx.entityType,
        action: tx.action,
        entityId: tx.entityId,
        data: tx.action === 'D' ? null : tx.data,
      })),
    }, {
      headers: { 'X-Workspace-Id': workspaceId! },
    })
    return response.data.results
  }
}