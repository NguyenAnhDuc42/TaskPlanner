import {
  MetadataDB,
  TaskDB,
  SpaceDB,
  FolderDB,
  StatusDB,
  MemberDB,
  CommentDB,
  DocumentBlockDB,
  AssigneeDB,
  FavoriteDB,
  TransactionDB,
  FetchedContextDB,
  type TaskPlanDB,
} from "@/db";
import { TaskStore } from "./task.store";
import { SpaceStore } from "./space.store";
import { FolderStore } from "./folder.store";
import { MemberStore } from "./member.store";
import { StatusStore } from "./status.store";
import { CommentStore } from "./comment.store";
import { DocumentBlockStore } from "./document-block.store";
import { AssigneeStore } from "./assignee.store";
import { FavoriteStore } from "./favorite.store";

import type { IDBPDatabase } from "idb";
import { makeAutoObservable } from "mobx";
import { closeWorkspaceDB, openWorkspaceDB } from "@/db/schema";
import { createContext, useContext } from "react";

// Everything scoped to exactly one workspace, for exactly as long as that workspace is open —
// unlike RootStore (app-session lifetime), a fresh WorkspaceRootStore is constructed per
// workspace visit (see SyncProvider) and discarded on leaving, never mutated in place. workspaceId
// is immutable for the instance's whole life — there is no `switchWorkspace()`; switching
// workspaces means constructing a new instance, not reassigning fields on a shared one. This is
// what makes the earlier `currentWorkspaceId` async-hydration race structurally impossible: a
// component can't read a not-yet-hydrated WorkspaceRootStore, because the provider supplying it
// doesn't render until hydration is done.
export class WorkspaceRootStore {
  readonly workspaceId: string;

  // Stores
  taskStore = new TaskStore();
  spaceStore = new SpaceStore();
  folderStore = new FolderStore();
  memberStore = new MemberStore();
  statusStore = new StatusStore();
  commentStore = new CommentStore();
  documentBlockStore = new DocumentBlockStore();
  assigneeStore = new AssigneeStore();
  favoriteStore = new FavoriteStore();

  // DBs
  taskDB: TaskDB | null = null;
  spaceDB: SpaceDB | null = null;
  folderDB: FolderDB | null = null;
  statusDB: StatusDB | null = null;
  memberDB: MemberDB | null = null;
  commentDB: CommentDB | null = null;
  documentBlockDB: DocumentBlockDB | null = null;
  assigneeDB: AssigneeDB | null = null;
  favoriteDB: FavoriteDB | null = null;
  metadataDB: MetadataDB | null = null;
  transactionDB: TransactionDB | null = null;
  fetchedContextDB: FetchedContextDB | null = null;

  private db: IDBPDatabase<TaskPlanDB> | null = null;

  constructor(workspaceId: string) {
    this.workspaceId = workspaceId;
    makeAutoObservable(this, {}, { autoBind: true });
  }

  // Opens the workspace's IndexedDB, wires up DB wrappers, and hydrates every store from local
  // data. Await this before rendering anything that reads from the instance.
  async hydrate(): Promise<void> {
    this.db = await openWorkspaceDB(this.workspaceId);

    this.taskDB = new TaskDB(this.db);
    this.spaceDB = new SpaceDB(this.db);
    this.folderDB = new FolderDB(this.db);
    this.statusDB = new StatusDB(this.db);
    this.memberDB = new MemberDB(this.db);
    this.commentDB = new CommentDB(this.db);
    this.documentBlockDB = new DocumentBlockDB(this.db);
    this.assigneeDB = new AssigneeDB(this.db);
    this.favoriteDB = new FavoriteDB(this.db);
    this.metadataDB = new MetadataDB(this.db, this.workspaceId);
    this.transactionDB = new TransactionDB(this.db);
    this.fetchedContextDB = new FetchedContextDB(this.db);

    const [tasks, spaces, folders, statuses, members, comments, blocks, assignees, favorites] = await Promise.all([
      this.taskDB.getAll(),
      this.spaceDB.getAll(),
      this.folderDB.getAll(),
      this.statusDB.getAll(),
      this.memberDB.getAll(),
      this.commentDB.getAll(),
      this.documentBlockDB.getAll(),
      this.assigneeDB.getAll(),
      this.favoriteDB.getAll(),
    ]);

    this.taskStore.hydrate(tasks);
    this.spaceStore.hydrate(spaces);
    this.folderStore.hydrate(folders);
    this.statusStore.hydrate(statuses);
    this.memberStore.hydrate(members);
    this.commentStore.hydrate(comments);
    this.documentBlockStore.hydrate(blocks);
    this.assigneeStore.hydrate(assignees);
    this.favoriteStore.hydrate(favorites);
  }

  // Closes the raw IDB handle. Call on unmount/workspace switch — the instance itself is then
  // just discarded (garbage collected), no in-place clearing needed since nothing else holds it.
  dispose(): void {
    closeWorkspaceDB(this.workspaceId);
  }
}

const WorkspaceRootStoreContext = createContext<WorkspaceRootStore | null>(null);
export const WorkspaceRootStoreProvider = WorkspaceRootStoreContext.Provider;

export function useWorkspaceRootStore(): WorkspaceRootStore {
  const store = useContext(WorkspaceRootStoreContext);
  if (!store) {
    throw new Error("useWorkspaceRootStore must be used within WorkspaceRootStoreProvider");
  }
  return store;
}

// Non-throwing variant for the handful of components that render both with and without a
// workspace open (e.g. the notification bell, which resolves @mention names when inside a
// workspace and falls back to a placeholder outside one).
export function useOptionalWorkspaceRootStore(): WorkspaceRootStore | null {
  return useContext(WorkspaceRootStoreContext);
}
