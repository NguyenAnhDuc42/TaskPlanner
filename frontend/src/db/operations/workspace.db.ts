import type { IDBPDatabase } from "idb";
import type { UserDB } from "../user-schema";
import type { WorkspaceRecord } from "@/types/workspace";

export class WorkspaceDB {
  private db: IDBPDatabase<UserDB>;

   constructor(db: IDBPDatabase<UserDB>) {
    this.db = db;
  }

  async getAll(): Promise<WorkspaceRecord[]> {
    return this.db.getAll("workspaces");
  }

  async put(workspace: WorkspaceRecord): Promise<void> {
    await this.db.put("workspaces", workspace);
  }

  async putMany(workspaces: WorkspaceRecord[]): Promise<void> {
    const tx = this.db.transaction("workspaces", "readwrite");
    await Promise.all(workspaces.map((w) => tx.store.put(w)));
    await tx.done;
  }

  async delete(id: string): Promise<void> {
    await this.db.delete("workspaces", id);
  }
}
