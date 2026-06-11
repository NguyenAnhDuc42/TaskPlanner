import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import type { SpaceRecord, FolderRecord, TaskRecord, AssigneeRecord, CommentRecord, AttachmentRecord } from "@/types/projects";
import type { MemberRecord, WorkspaceRecord, EntityAccessRecord } from "@/types/workspace";
import type { Status } from "@/types/status";
import type { DocumentBlockRecord } from "@/types/document";

// The transactional update packet (1 level deep flat entities for multiple rows and types)
export interface EntityBatchUpdate {
  spaces?: (Partial<SpaceRecord> & { id: string })[];
  folders?: (Partial<FolderRecord> & { id: string })[];
  tasks?: (Partial<TaskRecord> & { id: string })[];
  members?: (Partial<MemberRecord> & { id: string })[];
  assignees?: (Partial<AssigneeRecord> & { id: string })[];
  entityAccess?: (Partial<EntityAccessRecord> & { id: string })[];
  workspaces?: (Partial<WorkspaceRecord> & { id: string })[];
  statuses?: (Partial<Status> & { id: string })[];
  comments?: (Partial<CommentRecord> & { id: string })[];
  documentBlocks?: (Partial<DocumentBlockRecord> & { id: string })[];
  attachments?: (Partial<AttachmentRecord> & { id: string })[];
}

// The transactional deletion packet
export interface EntityBatchDelete {
  spaceIds?: string[];
  folderIds?: string[];
  taskIds?: string[];
  memberIds?: string[];
  assigneeIds?: string[];
  entityAccessIds?: string[];
  workspaceIds?: string[];
  statusIds?: string[];
  commentIds?: string[];
  documentBlockIds?: string[];
  attachmentIds?: string[];
}

// 1. Strict contract for all SignalR events (Zero loose types, decoupled from specific view features)
export interface SignalREvents {
  EntitiesUpdated: EntityBatchUpdate;
  EntitiesDeleted: EntityBatchDelete;
}

type SignalRListener<E extends keyof SignalREvents> = {
  eventName: E;
  callback: (data: SignalREvents[E]) => void;
};

class SignalRService {
  private connection: HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private readonly url: string = "https://localhost:7285/hubs/workspace";
  private pendingListeners: SignalRListener<keyof SignalREvents>[] = [];
  private reconnectCallbacks: Array<() => void> = [];

  public async startConnection(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    if (this.startPromise) {
      return this.startPromise;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(this.url, {
        withCredentials: true,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    // 2. Strong reconnect event handling
    this.connection.onreconnected((connectionId) => {
      console.log("[SignalR] Reconnected with ID:", connectionId);
      this.reconnectCallbacks.forEach((cb) => cb());
    });

    this.flushPendingListeners();

    // The startPromise now wraps the entire connection retry chain so concurrent callers always await resolution
    this.startPromise = new Promise<void>((resolve, reject) => {
      const tryStart = async () => {
        try {
          await this.connection!.start();
          console.log("[SignalR] Connection started");
          this.startPromise = null;
          resolve();
        } catch (err) {
          console.error("[SignalR] Error starting connection, retrying in 5s: " + err);
          setTimeout(() => tryStart(), 5000);
        }
      };

      try {
        tryStart();
      } catch (err) {
        this.startPromise = null;
        reject(err);
      }
    });

    return this.startPromise;
  }

  private flushPendingListeners(): void {
    if (!this.connection) return;
    
    this.pendingListeners.forEach(({ eventName, callback }) => {
      this.connection!.on(eventName, callback as (data: unknown) => void);
    });
    this.pendingListeners = [];
  }

  // 3. 100% Type-Safe Event subscription
  public on<E extends keyof SignalREvents>(
    eventName: E,
    callback: (data: SignalREvents[E]) => void
  ): void {
    if (!this.connection) {
      this.pendingListeners.push({ eventName, callback });
      return;
    }
    this.connection.on(eventName, callback as (data: unknown) => void);
  }

  public off<E extends keyof SignalREvents>(
    eventName: E,
    callback: (data: SignalREvents[E]) => void
  ): void {
    if (!this.connection) {
      this.pendingListeners = this.pendingListeners.filter(
        l => !(l.eventName === eventName && l.callback === callback)
      );
      return;
    }
    this.connection.off(eventName, callback as (data: unknown) => void);
  }

  public onReconnected(callback: () => void): void {
    this.reconnectCallbacks.push(callback);
  }

  public offReconnected(callback: () => void): void {
    this.reconnectCallbacks = this.reconnectCallbacks.filter(cb => cb !== callback);
  }

  public getConnectionId(): string | null {
    return this.connection?.connectionId || null;
  }

  public async invoke<T = void>(methodName: string, ...args: unknown[]): Promise<T> {
    if (!this.connection) {
      throw new Error("[SignalR] Connection not initialized");
    }
    return this.connection.invoke(methodName, ...args);
  }

  public async stopConnection(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.pendingListeners = [];
      this.reconnectCallbacks = [];
      this.startPromise = null;
    }
  }
}

export const signalRService = new SignalRService();
