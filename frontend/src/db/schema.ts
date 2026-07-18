import type { DocumentBlockRecord } from "@/types/document";
import type { AssigneeRecord, CommentRecord, DocumentRecord, FavoriteRecord, FolderRecord, SpaceRecord, TaskRecord } from "@/types/projects";
import type { Status } from "@/types/status";
import type { PendingTransaction, WorkspaceMetadata } from "@/types/sync";
import type { MemberRecord } from "@/types/workspace";
import { openDB, type DBSchema, type IDBPDatabase } from "idb";

export interface TaskPlanDB extends DBSchema {
  __metadata: {
    key:string;
    value: WorkspaceMetadata;
  };
  __fetched_contexts: {
    key: string;
    value: { context: string; fetchedAt: string };
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

  documents: {
    key: string;
    value: DocumentRecord;
    indexes: {
      'by-space': string
      'by-parent': string
    }
  }

  document_blocks:{
    key:string;
    value:DocumentBlockRecord
    indexes: {
      'by-document': string
    }
  }

  members:{
    key:string;
    value:MemberRecord
  }

  assignees:{
    key:string;
    value:AssigneeRecord
    indexes: {
      'by-task': string
    }
  }

  favorites: {
    key: string; // entityId — one favorite per entity per member
    value: FavoriteRecord
  }
}

const DB_VERSION = 6;
const dbCache = new Map<string, IDBPDatabase<TaskPlanDB>>()

export async function openWorkspaceDB(workspaceId:string) : Promise<IDBPDatabase<TaskPlanDB>> {
  const cached = dbCache.get(workspaceId)
  if (cached) return cached

  const db = await openDB<TaskPlanDB>(`taskplan_${workspaceId}`,DB_VERSION, {
    upgrade(db){
      if (!db.objectStoreNames.contains('__metadata')) {
        db.createObjectStore('__metadata')
      }

      if (!db.objectStoreNames.contains('__fetched_contexts')) {
        db.createObjectStore('__fetched_contexts')
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
        const documents = db.createObjectStore('documents', {keyPath: 'id'})
        documents.createIndex('by-space','spaceId')
        documents.createIndex('by-parent','parentDocumentId')
      }

      if (!db.objectStoreNames.contains('document_blocks')) {
        const document_blocks = db.createObjectStore('document_blocks',{keyPath: 'id'})
        document_blocks.createIndex('by-document','documentId')
      }

      if (!db.objectStoreNames.contains('members')) {
        db.createObjectStore('members',{keyPath:'id'})
      }

      if (!db.objectStoreNames.contains('assignees')) {
        const assignees = db.createObjectStore('assignees', {keyPath: 'id'})
        assignees.createIndex('by-task','taskId')
      }

      if (!db.objectStoreNames.contains('favorites')) {
        db.createObjectStore('favorites', {keyPath: 'entityId'})
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


export async function deleteWorkspaceDB(workspaceId: string): Promise<void> {
  closeWorkspaceDB(workspaceId)
  const { deleteDB } = await import('idb')
  await deleteDB(`taskplan_${workspaceId}`)
}
