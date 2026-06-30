import {
  MetadataDB,
  TaskDB,
  SpaceDB,
  FolderDB,
  StatusDB,
  MemberDB,
  CommentDB,
  DocumentDB,
  DocumentBlockDB,
  EntityAccessDB,
  TransactionDB,
  type TaskPlanDB
} from "@/db";
import { TaskStore } from "./task.store";
import { SpaceStore } from "./space.store";
import { FolderStore } from "./folder.store";
import { MemberStore } from "./member.store";
import { NotificationStore } from "./notification.store";
import { StatusStore } from "./status.store";
import { WorkspaceStore } from "./workspace.store";
import { CommentStore } from "./comment.store";
import { DocumentStore } from "./document.store";
import { DocumentBlockStore } from "./document-block.store";
import { EntityAccessStore } from "./entity-access.store";

import type { IDBPDatabase } from "idb";
import { makeAutoObservable } from "mobx";
import { closeWorkspaceDB, openWorkspaceDB } from "@/db/schema";
import { closeUserDB, openUserDB } from "@/db/user-schema";
import { WorkspaceDB, NotificationDB, type UserDB } from "@/db";
import { createContext, useContext } from "react";

export class RootStore {
  currentWorkspaceId: string | null = null;
  isOnline: boolean = typeof navigator !== 'undefined' ? navigator.onLine : true;

  currentUserId: string | null = null;

  // Stores
  taskStore = new TaskStore();
  spaceStore = new SpaceStore();
  folderStore = new FolderStore();
  memberStore = new MemberStore();
  notificationStore = new NotificationStore();
  statusStore = new StatusStore();
  workspaceStore = new WorkspaceStore();
  commentStore = new CommentStore();
  documentStore = new DocumentStore();
  documentBlockStore = new DocumentBlockStore();
  entityAccessStore = new EntityAccessStore();

  // DBs
  taskDB: TaskDB | null = null;
  spaceDB: SpaceDB | null = null;
  folderDB: FolderDB | null = null;
  statusDB: StatusDB | null = null;
  memberDB: MemberDB | null = null;
  commentDB: CommentDB | null = null;
  documentDB: DocumentDB | null = null;
  documentBlockDB: DocumentBlockDB | null = null;
  entityAccessDB: EntityAccessDB | null = null;
  metadataDB: MetadataDB | null = null;
  transactionDB: TransactionDB | null = null;
  workspaceDB: WorkspaceDB | null = null;
  notificationDB: NotificationDB | null = null;

  private db: IDBPDatabase<TaskPlanDB> | null = null;
  private userDb: IDBPDatabase<UserDB> | null = null;

  constructor() {
    makeAutoObservable(this, {}, { autoBind: true });

    if (typeof window !== 'undefined') {
      window.addEventListener('online', () => this.setOnline(true));
      window.addEventListener('offline', () => this.setOnline(false));
    }
  }

  setOnline(status: boolean) {
    this.isOnline = status;
  }

  async initUser(userId: string): Promise<void> {
    if (this.currentUserId === userId) return;

    if (this.currentUserId) {
      closeUserDB(this.currentUserId);
    }

    // Clear user-level stores
    this.workspaceStore.clear();
    this.notificationStore.clear();

    this.userDb = await openUserDB(userId);
    this.currentUserId = userId;

    // Init User-level DB wrappers
    this.workspaceDB = new WorkspaceDB(this.userDb);
    this.notificationDB = new NotificationDB(this.userDb);

    // Hydrate User-level stores
    const [workspaces, notifications] = await Promise.all([
      this.workspaceDB.getAll(),
      this.notificationDB.getAll()
    ]);

    this.workspaceStore.hydrate(workspaces);
    this.notificationStore.hydrate(notifications);
  }

  async switchWorkspace(workspaceId: string): Promise<void> {
    if (this.currentWorkspaceId) {
      closeWorkspaceDB(this.currentWorkspaceId);
    }

    // Clear all in-memory stores before switching
    this.taskStore.clear();
    this.spaceStore.clear();
    this.folderStore.clear();
    this.memberStore.clear();
    // Do not clear notificationStore here, as it belongs to the User, not a specific workspace.
    this.statusStore.clear();
    this.commentStore.clear();
    this.documentStore.clear();
    this.documentBlockStore.clear();
    this.entityAccessStore.clear();

    this.db = await openWorkspaceDB(workspaceId);
    this.currentWorkspaceId = workspaceId;

    // Initialize DB operations
    this.taskDB = new TaskDB(this.db);
    this.spaceDB = new SpaceDB(this.db);
    this.folderDB = new FolderDB(this.db);
    this.statusDB = new StatusDB(this.db);
    this.memberDB = new MemberDB(this.db);
    this.commentDB = new CommentDB(this.db);
    this.documentDB = new DocumentDB(this.db);
    this.documentBlockDB = new DocumentBlockDB(this.db);
    this.entityAccessDB = new EntityAccessDB(this.db);
    this.metadataDB = new MetadataDB(this.db, workspaceId);
    this.transactionDB = new TransactionDB(this.db);

    await this.hydrateFromLocal();
  }

  private async hydrateFromLocal(): Promise<void> {
    if (!this.db) return;

    // Fetch all records from IndexedDB in parallel for fast loading
    const [tasks, spaces, folders, statuses, members, comments, documents, blocks, accesses] = await Promise.all([
      this.taskDB!.getAll(),
      this.spaceDB!.getAll(),
      this.folderDB!.getAll(),
      this.statusDB!.getAll(),
      this.memberDB!.getAll(),
      this.commentDB!.getAll(),
      this.documentDB!.getAll(),
      this.documentBlockDB!.getAll(),
      this.entityAccessDB!.getAll(),
    ]);

    // Populate MobX stores with initial data
    this.taskStore.hydrate(tasks);
    this.spaceStore.hydrate(spaces);
    this.folderStore.hydrate(folders);
    this.statusStore.hydrate(statuses);
    this.memberStore.hydrate(members);
    this.commentStore.hydrate(comments);
    this.documentStore.hydrate(documents);
    this.documentBlockStore.hydrate(blocks);
    this.entityAccessStore.hydrate(accesses);
  }
}

const RootStoreContext = createContext<RootStore | null>(null);
export const RootStoreProvider = RootStoreContext.Provider;

export function useStore(): RootStore {
  const store = useContext(RootStoreContext);
  if (!store) {
    throw new Error('useStore must be used within RootStoreProvider')
  }
  return store
}

// Convenience hooks
export function useTaskStore(): TaskStore { return useStore().taskStore }
export function useSpaceStore(): SpaceStore { return useStore().spaceStore }
export function useFolderStore(): FolderStore { return useStore().folderStore }
export function useMemberStore(): MemberStore { return useStore().memberStore }
export function useNotificationStore(): NotificationStore { return useStore().notificationStore }
export function useStatusStore(): StatusStore { return useStore().statusStore }
export function useWorkspaceStore(): WorkspaceStore { return useStore().workspaceStore }
export function useCommentStore(): CommentStore { return useStore().commentStore }
export function useDocumentStore(): DocumentStore { return useStore().documentStore }
export function useDocumentBlockStore(): DocumentBlockStore { return useStore().documentBlockStore }
export function useEntityAccessStore(): EntityAccessStore { return useStore().entityAccessStore }
export function useIsOnline(): boolean { return useStore().isOnline }
