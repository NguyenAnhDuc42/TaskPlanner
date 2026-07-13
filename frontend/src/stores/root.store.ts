import { NotificationStore } from "./notification.store";
import { WorkspaceStore } from "./workspace.store";

import { makeAutoObservable } from "mobx";
import { closeWorkspaceDB, deleteWorkspaceDB } from "@/db/schema";
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

  async markWorkspaceAccessRevoked(id: string): Promise<void> {
    this.workspaceStore.markAccessRevoked(id);
    const updated = this.workspaceStore.getById(id);
    if (updated) await this.workspaceDB?.put(updated);
  }

  async clearAllLocalData(): Promise<void> {
    for (const w of this.workspaceStore.all) closeWorkspaceDB(w.id);
    if (this.currentUserId) closeUserDB(this.currentUserId);

    const workspaceIds = this.workspaceStore.all.map((w) => w.id);
    await Promise.all(workspaceIds.map((id) => deleteWorkspaceDB(id)));
    if (this.currentUserId) await deleteUserDB(this.currentUserId);

    if (typeof indexedDB.databases === "function") {
      const { deleteDB } = await import("idb");
      const allDbs = await indexedDB.databases();
      const staleDbs = allDbs.filter(
        (d) => d.name && (d.name.startsWith("taskplan_") || d.name.startsWith("user_")),
      );
      await Promise.all(staleDbs.map((d) => deleteDB(d.name!)));
    }

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
