export interface NotificationRecord {
  id: string;
  type: string;
  entityType?: string;
  entityId?: string;
  workspaceId?: string;
  title: string;
  body?: string;
  isRead: boolean;
  createdAt: string;
  actorName?: string;
}
