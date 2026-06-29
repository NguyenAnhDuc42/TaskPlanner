import {
  HubConnectionBuilder,
  HubConnectionState,
  type HubConnection,
} from '@microsoft/signalr'
import type { RootStore } from '@/stores/root.store'
import type { DeltaPayload, DeltaBatchPayload } from '@/types/sync/delta'
import { applyDelta, applyDeltaBatch } from './delta-handler'
import { TransactionQueue } from './transaction-queue'
import type { TaskRecord } from '@/types/projects'
import type { PendingTransaction } from '@/types/sync'
import { api } from '@/lib/api-client'

interface BootstrapResponse {
  lastSyncId: number
  databaseVersion: number
  tasks: Record<string, unknown>[]
  // spaces: Record<string, unknown>[]
  // folders: Record<string, unknown>[]
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
    const { taskDB, metadataDB } = this.rootStore
    await taskDB!.putMany(data.tasks as unknown as TaskRecord[])
    // await spaceDB.putMany(data.spaces as any[])

    // Set metadata
    await metadataDB!.setFullBootstrap(data.lastSyncId, data.databaseVersion)

    // Hydrate stores from what we just saved
    const tasks = await taskDB!.getAll()
    this.rootStore.taskStore.hydrate(tasks)
  }

  // ── SignalR connection ──

  private async connect(workspaceId: string): Promise<void> {
    const lastSyncId = await this.rootStore.metadataDB!.getLastSyncId()

    this.connection = new HubConnectionBuilder()
      .withUrl(
        `${api.defaults.baseURL?.replace('/api', '')}/hubs/sync?workspaceId=${workspaceId}&lastSyncId=${lastSyncId}`
      )
      .withAutomaticReconnect()
      .build()

    // Live deltas
    this.connection.on('Delta', (delta: DeltaPayload) => {
      applyDelta(this.rootStore, delta)
    })

    // Batch deltas (catch-up on connect/reconnect)
    this.connection.on('DeltaBatch', (payload: DeltaBatchPayload) => {
      applyDeltaBatch(this.rootStore, payload.actions)
    })

    // On reconnect, server sends catch-up automatically
    // but we also flush pending transactions
    this.connection.onreconnected(async () => {
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
    const base = `/${tx.entityType.toLowerCase()}s`
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