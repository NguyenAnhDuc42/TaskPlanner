

export interface DeltaPayload{
  syncId: number;
  action: SyncAction;
  entityType: SyncEntityType;
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
export type SyncEntityType =
'Workspace'|
'Space'|
'Folder'|
'Task'|
'Status'|
'Comment'|
'Document'|
'DocumentBlock'|
'Member'|
'EntityAccess'|
'Assignee'