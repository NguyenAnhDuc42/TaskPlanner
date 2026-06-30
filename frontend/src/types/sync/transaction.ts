import type { SyncEntityType, SyncAction } from "./delta";

export type TransactionStatus = "pending" | "in_flight" | "failed";

export interface PendingTransaction {
  id: string;
  action: SyncAction;
  entityType: SyncEntityType;
  entityId:string
  data: Record<string,unknown>;
  previousData:  Record<string,unknown> | null;
  createdAt: number;
  retryCount: number;
  status: TransactionStatus;
}
