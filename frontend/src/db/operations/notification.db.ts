import type { IDBPDatabase } from "idb";
import type { UserDB } from "../user-schema";
import type { NotificationRecord } from "@/types/notification-record";

export class NotificationDB {
  private db: IDBPDatabase<UserDB>;

  constructor(db: IDBPDatabase<UserDB>) {
    this.db = db;
  }

  async getAll(): Promise<NotificationRecord[]> {
    return this.db.getAll("notifications");
  }

  async put(notification: NotificationRecord): Promise<void> {
    await this.db.put("notifications", notification);
  }

  async putMany(notifications: NotificationRecord[]): Promise<void> {
    const tx = this.db.transaction("notifications", "readwrite");
    await Promise.all(notifications.map((n) => tx.store.put(n)));
    await tx.done;
  }

  async delete(id: string): Promise<void> {
    await this.db.delete("notifications", id);
  }
}
