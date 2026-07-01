import {
  HubConnectionBuilder,
  HubConnectionState,
  type HubConnection,
} from '@microsoft/signalr'
import type { RootStore } from '@/stores/root.store'
import type { DeltaPayload, DeltaBatchPayload } from '@/types/sync/delta'
import { applyDelta, applyDeltaBatch } from './delta-handler'
import { TransactionQueue } from './transaction-queue'
import type { TaskRecord, SpaceRecord, FolderRecord } from '@/types/projects'
import type { Status } from '@/types/status'
import type { PendingTransaction } from '@/types/sync'
import { api } from '@/lib/api-client'
import { devLog } from './dev-log'

interface BootstrapResponse {
  lastSyncId: number
  databaseVersion: number
  tasks: Record<string, unknown>[]
  spaces: Record<string, unknown>[]
  folders: Record<string, unknown>[]
  statuses: Record<string, unknown>[]
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

  /**
   * Manually retry all pending transactions. Normally happens automatically
   * on SignalR reconnect, but useful to trigger explicitly (e.g. after
   * coming back online without a real connection drop).
   */
  async flushQueue(): Promise<void> {
    await this.queue.flush((tx) => this.sendTransaction(tx))
  }

  async forceBootstrap(workspaceId: string): Promise<void> {
    // Hard reset to server ground truth — clears local state first
    const { taskDB, spaceDB, folderDB, statusDB } = this.rootStore
    await Promise.all([taskDB!.clear(), spaceDB!.clear(), folderDB!.clear(), statusDB!.clear()])
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
    const lastSyncId = await this.rootStore.metadataDB!.getLastSyncId()

    if (lastSyncId === 0) {
      // First time — full bootstrap
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
    const { taskDB, spaceDB, folderDB, statusDB, metadataDB } = this.rootStore
    await Promise.all([
      taskDB!.putMany(data.tasks as unknown as TaskRecord[]),
      spaceDB!.putMany(data.spaces as unknown as SpaceRecord[]),
      folderDB!.putMany(data.folders as unknown as FolderRecord[]),
      statusDB!.putMany(data.statuses as unknown as Status[]),
    ])

    // Set metadata
    await metadataDB!.setFullBootstrap(data.lastSyncId, data.databaseVersion)

    // Hydrate stores from what we just saved
    const [tasks, spaces, folders, statuses] = await Promise.all([
      taskDB!.getAll(),
      spaceDB!.getAll(),
      folderDB!.getAll(),
      statusDB!.getAll(),
    ])
    this.rootStore.taskStore.hydrate(tasks)
    this.rootStore.spaceStore.hydrate(spaces)
    this.rootStore.folderStore.hydrate(folders)
    this.rootStore.statusStore.hydrate(statuses)
  }

  // ── SignalR connection ──

  private async connect(workspaceId: string): Promise<void> {
    const lastSyncId = await this.rootStore.metadataDB!.getLastSyncId()

    this.connection = new HubConnectionBuilder()
      .withUrl(
        `${api.defaults.baseURL?.replace('/api', '')}/hubs/sync?workspaceId=${workspaceId}&lastSyncId=${lastSyncId}`
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
      await this.queue.flush((tx) => this.sendTransaction(tx))
    })

    await this.connection.start()

    // Flush any pending transactions from previous session
    await this.queue.flush((tx) => this.sendTransaction(tx))
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

  private async sendTransaction(tx: PendingTransaction): Promise<void> {
    const workspaceId = this.rootStore.currentWorkspaceId
    const config = this.getRequestConfig(tx)

    await api.request({
      method: config.method,
      url: config.url,
      headers: {
        'X-Workspace-Id': workspaceId!,
        'X-Client-Trace-Id': tx.id,
      },
      data: tx.action === 'D' ? undefined : tx.data,
    })
  }

  private getRequestConfig(tx: PendingTransaction): { method: string; url: string } {
    // New sync-flow slices live under /sync to coexist with the old REST
    // endpoints during migration. Once a slice's old route is removed,
    // it can drop the suffix.
    const base = `/${tx.entityType.toLowerCase()}s/sync`
    switch (tx.action) {
      case "C":
        return { method: 'POST', url: base }
      case "U":
        return { method: 'PUT', url: `${base}/${tx.entityId}` }
      case "D":
        return { method: 'DELETE', url: `${base}/${tx.entityId}` }
      default:
        return { method: 'POST', url: base }
    }
  }
}