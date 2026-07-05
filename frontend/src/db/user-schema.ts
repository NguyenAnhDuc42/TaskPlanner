import type { NotificationRecord } from "@/types/notification-record";
import type { WorkspaceRecord } from "@/types/workspace";
import { openDB, type DBSchema, type IDBPDatabase } from "idb";

export interface UserDB extends DBSchema {
  workspaces: {
    key: string;
    value: WorkspaceRecord;
  };

  notifications: {
    key: string;
    value: NotificationRecord;
    indexes: {
      "by-read": number;
    };
  };
}

const DB_VERSION = 1;
const dbCache = new Map<string, IDBPDatabase<UserDB>>();

export async function openUserDB(userId: string): Promise<IDBPDatabase<UserDB>> {
  const cached = dbCache.get(userId);
  if (cached) return cached;

  const db = await openDB<UserDB>(`user_${userId}`, DB_VERSION, {
    upgrade(db) {
    db.createObjectStore('workspaces',{keyPath:'id'})

    const notifications = db.createObjectStore("notifications", { keyPath: "id" });
    notifications.createIndex("by-read", "isRead");
    
  }});

  dbCache.set(userId, db);
  return db;
}

export function closeUserDB(userId: string): void {
  const db = dbCache.get(userId);
  if (db) {
    db.close();
    dbCache.delete(userId);
  }
}

// Wipes this user's local IndexedDB (workspaces list, notifications) — used on logout so
// stale data isn't left readable via devtools on a shared/public machine.
export async function deleteUserDB(userId: string): Promise<void> {
  closeUserDB(userId);
  const { deleteDB } = await import("idb");
  await deleteDB(`user_${userId}`);
}
