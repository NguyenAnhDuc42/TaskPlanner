import { NotificationStore } from "./notification.store";
import { WorkspaceStore } from "./workspace.store";

import { makeAutoObservable } from "mobx";
import { deleteWorkspaceDB } from "@/db/schema";
import { closeUserDB, deleteUserDB, openUserDB } from "@/db/user-schema";
import { WorkspaceDB, NotificationDB, type UserDB } from "@/db";
import type { IDBPDatabase } from "idb";
import { createContext, useContext } from "react";

// User-scope only — persists for the whole app session regardless of which workspace (if any)
// is open. Workspace-scope state (tasks/spaces/folders/etc.) lives on WorkspaceRootStore
// (workspace-root.store.ts) instead, constructed fresh per workspace visit rather than mutated
// in place on this long-lived instance.
export class RootStore {
  isOnline: boolean = typeof navigator !== 'undefined' ? navigator.onLine : true;

  currentUserId: string | null = null;

  workspaceStore = new WorkspaceStore();
  notificationStore = new NotificationStore();

  workspaceDB: WorkspaceDB | null = null;
  notificationDB: NotificationDB | null = null;

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

    this.workspaceStore.clear();
    this.notificationStore.clear();

    this.userDb = await openUserDB(userId);
    this.currentUserId = userId;

    this.workspaceDB = new WorkspaceDB(this.userDb);
    this.notificationDB = new NotificationDB(this.userDb);

    const [workspaces, notifications] = await Promise.all([
      this.workspaceDB.getAll(),
      this.notificationDB.getAll()
    ]);

    this.workspaceStore.hydrate(workspaces);
    this.notificationStore.hydrate(notifications);
  }

  // Wipes every local trace of the current user on this device — the user-level DB
  // (workspaces/notifications) plus every workspace DB they'd cached — so logging out on a
  // shared/public machine doesn't leave the previous session's data sitting in IndexedDB.
  // deleteWorkspaceDB takes just an id, not a live WorkspaceRootStore instance, so this works
  // regardless of whether a workspace happens to be open right now.
  async clearAllLocalData(): Promise<void> {
    const workspaceIds = this.workspaceStore.all.map((w) => w.id);
    await Promise.all(workspaceIds.map((id) => deleteWorkspaceDB(id)));

    if (this.currentUserId) await deleteUserDB(this.currentUserId);

    this.workspaceStore.clear();
    this.notificationStore.clear();

    this.currentUserId = null;
    this.userDb = null;
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

// Module-level reference to the one RootStore instance, set once by AppShell — lets plain
// modules that can't use React context (lib/api-client/interceptors.ts, sync/delta-handler.ts)
// reach the user-scope store without it being threaded through every constructor.
let activeRootStore: RootStore | null = null;
export function setActiveRootStore(store: RootStore | null): void {
  activeRootStore = store;
}
export function getActiveRootStore(): RootStore | null {
  return activeRootStore;
}
