

export interface DeltaPayload{
  syncId: number;
  action: SyncAction;
  entityType: EntityType;
  entityId: string;
  data: Record<string,unknown>;
  clientTraceId?: string;
}

export interface DeltaBatchPayload  {
  actions: DeltaPayload[];
  databaseVersion: number;
  latestSyncId: number;
}


export type SyncAction ='C'|'U'|'D'
export type EntityType =
'Space'|
'Folder'|
'Task'|
'Status'|
'Comment'|
'DocumentBlock'|
'Member'|
'EntityAccess'