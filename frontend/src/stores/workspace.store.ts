import type { WorkspaceRecord } from "@/types/workspace";
import { makeAutoObservable, observable } from "mobx";

export class WorkspaceStore {
  // deep: false — records replaced wholesale, never mutated in place. See DocumentBlockStore.
  workspaces = observable.map<string, WorkspaceRecord>({}, { deep: false });

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

  markAccessRevoked(id: string): void {
    const existing = this.workspaces.get(id);
    if (existing) this.workspaces.set(id, { ...existing, accessRevoked: true });
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
