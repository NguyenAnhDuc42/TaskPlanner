import type { DocumentBlockRecord, DocumentRecord } from "@/types/document";
import type { CommentRecord, FolderRecord, SpaceRecord, TaskRecord } from "@/types/projects";
import type { Status } from "@/types/status";
import type { PendingTransaction, WorkspaceMetadata } from "@/types/sync";
import type { EntityAccessRecord, MemberRecord } from "@/types/workspace";
import { openDB, type DBSchema, type IDBPDatabase } from "idb";

export interface TaskPlanDB extends DBSchema {
  __metadata: {
    key:string;
    value: WorkspaceMetadata;
  };
  __transactions: {
    key:string;
    value: PendingTransaction;
    indexes: {
      'by-entity': [string,string]  //entityId and entityType
      'by-status': string
    }
  }

  spaces : {
    key:string;
    value: SpaceRecord;
    
  }

  folders: {
    key: string;
    value: FolderRecord;
    indexes: {
      'by-space': string
    }
  };

  tasks: {
    key: string;
    value: TaskRecord;
    indexes: {
      'by-space': string
      'by-folder': string
      'by-parent-task': string
      'by-status': string
    }
  };

  comments: {
    key:string ;
    value: CommentRecord;
    indexes: {
      'by-task': string
    }
  }

  statuses:{
    key:string;
    value: Status;
    indexes: {
      'by-space': string
    }
  }

  documents:{
    key:string;
    value:DocumentRecord
  }

  document_blocks:{
    key:string;
    value:DocumentBlockRecord
    indexes: {
      'by-document': string
    }
  }

  entity_access:{
    key:string;
    value:EntityAccessRecord
    indexes: {
      'by-space': string
    }
  }

  members:{
    key:string;
    value:MemberRecord
  }
}

const DB_VERSION = 2;
const dbCache = new Map<string, IDBPDatabase<TaskPlanDB>>()

export async function openWorkspaceDB(workspaceId:string) : Promise<IDBPDatabase<TaskPlanDB>> {
  const cached = dbCache.get(workspaceId)
  if (cached) return cached

  const db = await openDB<TaskPlanDB>(`taskplan_${workspaceId}`,DB_VERSION, {
    upgrade(db){
      if (!db.objectStoreNames.contains('__metadata')) {
        db.createObjectStore('__metadata')
      }

      if (!db.objectStoreNames.contains('__transactions')) {
        const transactions = db.createObjectStore('__transactions' , {keyPath: 'id'})
        transactions.createIndex('by-status','status')
        transactions.createIndex('by-entity',['entityId','entityType'])
      }

      if (!db.objectStoreNames.contains('spaces')) {
        db.createObjectStore('spaces' , {keyPath: 'id'})
      }

      if (!db.objectStoreNames.contains('folders')) {
        const folders = db.createObjectStore('folders' , {keyPath : 'id'})
        folders.createIndex('by-space','spaceId')
      }

      if (!db.objectStoreNames.contains('tasks')) {
        const tasks = db.createObjectStore('tasks' , {keyPath: 'id'})
        tasks.createIndex('by-space','spaceId')
        tasks.createIndex('by-folder','folderId')
        tasks.createIndex('by-parent-task','parentTaskId')
        tasks.createIndex('by-status','statusId')
      }

      if (!db.objectStoreNames.contains('comments')) {
        const comments = db.createObjectStore('comments', {keyPath : 'id'})
        comments.createIndex('by-task','taskId')
      }

      if (!db.objectStoreNames.contains('statuses')) {
        const statuses = db.createObjectStore('statuses', {keyPath : 'id'})
        statuses.createIndex('by-space','spaceId')
      }

      if (!db.objectStoreNames.contains('documents')) {
        db.createObjectStore('documents',{keyPath: 'id'})
      }

      if (!db.objectStoreNames.contains('document_blocks')) {
        const document_blocks = db.createObjectStore('document_blocks',{keyPath: 'id'})
        document_blocks.createIndex('by-document','documentId')
      }

      if (!db.objectStoreNames.contains('entity_access')) {
        const entity_access = db.createObjectStore('entity_access', {keyPath: 'id'})
        entity_access.createIndex('by-space','spaceId')
      }

      if (!db.objectStoreNames.contains('members')) {
        db.createObjectStore('members',{keyPath:'id'})
      }
    }
  })

  dbCache.set(workspaceId,db)
  return db
}

export function closeWorkspaceDB(workspaceId:string): void{
  const db = dbCache.get(workspaceId)
  if (db){
    db.close()
    dbCache.delete(workspaceId)
  }
}
