import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel,
} from "@microsoft/signalr";
import type { SpaceRecord, FolderRecord, TaskRecord, AssigneeRecord, CommentRecord, AttachmentRecord } from "@/types/projects";
import type { MemberRecord, WorkspaceRecord, EntityAccessRecord } from "@/types/workspace";
import type { Status } from "@/types/status";
import type { DocumentBlockRecord } from "@/types/document";
import type { NotificationRecord } from "@/types/notification-record";

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
  notifications?: NotificationRecord[];
}

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

export interface SignalREvents {
  EntitiesUpdated: EntityBatchUpdate;
  EntitiesDeleted: EntityBatchDelete;
  NewNotification: NotificationRecord;
  AccessRevoked: { spaceId?: string };
  AccessGranted: { spaceId?: string };
}

type AnySignalRListener = { eventName: string; callback: (data: unknown) => void };

class SignalRService {
  private connection: HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private readonly url: string = `${import.meta.env.VITE_API_URL ?? ""}/hubs/workspace`;
  private pendingListeners: AnySignalRListener[] = [];
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
        transport: HttpTransportType.WebSockets,
        skipNegotiation: true,
      })
      .withAutomaticReconnect([0, 1000, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Information)
      .build();

    this.connection.onreconnected((connectionId) => {
      console.log("[SignalR] Reconnected with ID:", connectionId);
      this.reconnectCallbacks.forEach((cb) => cb());
    });

    this.flushPendingListeners();

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


  public on<E extends keyof SignalREvents>(
    eventName: E,
    callback: (data: SignalREvents[E]) => void
  ): void {
    if (!this.connection) {
      this.pendingListeners.push({ eventName, callback: callback as (data: unknown) => void });
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
        l => !(l.eventName === eventName && l.callback === (callback as (data: unknown) => void))
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

  public registerVisibilityReconnect(): () => void {
    const handler = () => {
      if (document.visibilityState === "visible" &&
          this.connection?.state !== HubConnectionState.Connected &&
          this.connection?.state !== HubConnectionState.Connecting) {
        this.startConnection();
      }
    };
    document.addEventListener("visibilitychange", handler);
    return () => document.removeEventListener("visibilitychange", handler);
  }
}

export const signalRService = new SignalRService();
