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
import { DocumentStore } from "./document.store";
import { DocumentBlockStore } from "./document-block.store";
import { AssigneeStore } from "./assignee.store";
import { FavoriteStore } from "./favorite.store";

import type { IDBPDatabase } from "idb";
import { makeAutoObservable } from "mobx";
import { closeWorkspaceDB, openWorkspaceDB } from "@/db/schema";
import { createContext, useContext } from "react";

export class WorkspaceRootStore {
  readonly workspaceId: string;

  taskStore = new TaskStore();
  spaceStore = new SpaceStore();
  folderStore = new FolderStore();
  memberStore = new MemberStore();
  statusStore = new StatusStore();
  commentStore = new CommentStore();
  documentStore = new DocumentStore();
  documentBlockStore = new DocumentBlockStore();
  assigneeStore = new AssigneeStore();
  favoriteStore = new FavoriteStore();

  taskDB: TaskDB | null = null;
  spaceDB: SpaceDB | null = null;
  folderDB: FolderDB | null = null;
  statusDB: StatusDB | null = null;
  memberDB: MemberDB | null = null;
  commentDB: CommentDB | null = null;
  documentDB: DocumentDB | null = null;
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

  async hydrate(): Promise<void> {
    this.db = await openWorkspaceDB(this.workspaceId);

    this.taskDB = new TaskDB(this.db);
    this.spaceDB = new SpaceDB(this.db);
    this.folderDB = new FolderDB(this.db);
    this.statusDB = new StatusDB(this.db);
    this.memberDB = new MemberDB(this.db);
    this.commentDB = new CommentDB(this.db);
    this.documentDB = new DocumentDB(this.db);
    this.documentBlockDB = new DocumentBlockDB(this.db);
    this.assigneeDB = new AssigneeDB(this.db);
    this.favoriteDB = new FavoriteDB(this.db);
    this.metadataDB = new MetadataDB(this.db, this.workspaceId);
    this.transactionDB = new TransactionDB(this.db);
    this.fetchedContextDB = new FetchedContextDB(this.db);

    const [tasks, spaces, folders, statuses, members, comments, assignees, favorites, documents] = await Promise.all([
      this.taskDB.getAll(),
      this.spaceDB.getAll(),
      this.folderDB.getAll(),
      this.statusDB.getAll(),
      this.memberDB.getAll(),
      this.commentDB.getAll(),
      this.assigneeDB.getAll(),
      this.favoriteDB.getAll(),
      this.documentDB.getAll(),
    ]);

    this.taskStore.hydrate(tasks);
    this.spaceStore.hydrate(spaces);
    this.folderStore.hydrate(folders);
    this.statusStore.hydrate(statuses);
    this.memberStore.hydrate(members);
    this.commentStore.hydrate(comments);
    this.assigneeStore.hydrate(assignees);
    this.favoriteStore.hydrate(favorites);
    this.documentStore.hydrate(documents);
  }

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

export function useOptionalWorkspaceRootStore(): WorkspaceRootStore | null {
  return useContext(WorkspaceRootStoreContext);
}
