import type { NotificationRecord } from "@/types/notification-record";
import { makeAutoObservable, observable } from "mobx";

export class NotificationStore {
  // deep: false — records replaced wholesale, never mutated in place. See DocumentBlockStore.
  notifications = observable.map<string, NotificationRecord>({}, { deep: false });

  constructor() {
    makeAutoObservable(this);
  }

  get all(): NotificationRecord[] {
    return Array.from(this.notifications.values()).sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );
  }

  get unreadCount(): number {
    return this.all.filter((n) => !n.isRead).length;
  }

  getById(id: string): NotificationRecord | undefined {
    return this.notifications.get(id);
  }

  upsert(notification: NotificationRecord): void {
    this.notifications.set(notification.id, notification);
  }

  upsertMany(notifications: NotificationRecord[]): void {
    for (const n of notifications) this.notifications.set(n.id, n);
  }

  markRead(id: string): void {
    const existing = this.notifications.get(id);
    if (existing) this.notifications.set(id, { ...existing, isRead: true });
  }

  remove(id: string): void {
    this.notifications.delete(id);
  }

  hydrate(notifications: NotificationRecord[]): void {
    this.notifications.clear();
    for (const n of notifications) this.notifications.set(n.id, n);
  }

  clear(): void {
    this.notifications.clear();
  }
}
