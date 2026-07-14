import { makeAutoObservable, observable } from "mobx";
import type { AssigneeRecord } from "@/types/projects/assignee-record";

const EMPTY_ASSIGNEES: AssigneeRecord[] = [];

export class AssigneeStore {
  // deep: false — records replaced wholesale, never mutated in place. See DocumentBlockStore.
  assignees = observable.map<string, AssigneeRecord>({}, { deep: false });

  constructor() {
    makeAutoObservable(this);
  }

  get all(): AssigneeRecord[] {
    return Array.from(this.assignees.values());
  }

  // Computed index — cached until the map changes; returned arrays are shared. See TaskStore.
  private get byTaskIndex(): Map<string, AssigneeRecord[]> {
    const index = new Map<string, AssigneeRecord[]>();
    for (const assignee of this.assignees.values()) {
      const list = index.get(assignee.taskId);
      if (list) list.push(assignee);
      else index.set(assignee.taskId, [assignee]);
    }
    return index;
  }

  getById(id: string): AssigneeRecord | undefined {
    return this.assignees.get(id);
  }

  getByTask(taskId: string): AssigneeRecord[] {
    return this.byTaskIndex.get(taskId) ?? EMPTY_ASSIGNEES;
  }

  getByMember(workspaceMemberId: string): AssigneeRecord[] {
    return this.all.filter((a) => a.workspaceMemberId === workspaceMemberId);
  }

  upsert(assignee: AssigneeRecord): void {
    this.assignees.set(assignee.id, assignee);
  }

  remove(id: string): void {
    this.assignees.delete(id);
  }

  hydrate(assignees: AssigneeRecord[]): void {
    this.assignees.clear();
    for (const a of assignees) {
      this.assignees.set(a.id, a);
    }
  }

  clear(): void {
    this.assignees.clear();
  }
}
