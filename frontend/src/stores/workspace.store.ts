import type { WorkspaceRecord } from "@/types/workspace";
import { makeAutoObservable, observable } from "mobx";

export class WorkspaceStore {
  workspaces = observable.map<string, WorkspaceRecord>();

  constructor() {
    makeAutoObservable(this);
  }

  get all(): WorkspaceRecord[] {
    return Array.from(this.workspaces.values());
  }

  getById(id: string): WorkspaceRecord | undefined {
    return this.workspaces.get(id);
  }

  upsert(workspace: WorkspaceRecord): void {
    this.workspaces.set(workspace.id, workspace);
  }

  remove(id: string): void {
    this.workspaces.delete(id);
  }

  hydrate(workspaces: WorkspaceRecord[]): void {
    this.workspaces.clear();
    for (const w of workspaces) {
      this.workspaces.set(w.id, w);
    }
  }

  clear(): void {
    this.workspaces.clear();
  }
}
