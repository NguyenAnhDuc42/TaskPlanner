import type { EntityType, SyncAction } from "./delta";

export type TransactionStatus = "pending" | "in_flight" | "failed";

export interface PendingTransaction {
  id: string;
  action: SyncAction;
  entityType: EntityType;
  entityId:string
  data: Record<string,unknown>;
  previousData:  Record<string,unknown> | null;
  createdAt: number;
  retryCount: number;
  status: TransactionStatus;
}
