import { makeAutoObservable, observable } from "mobx";
import type { AssigneeRecord } from "@/types/projects/assignee-record";

export class AssigneeStore {
  assignees = observable.map<string, AssigneeRecord>();

  constructor() {
    makeAutoObservable(this);
  }

  get all(): AssigneeRecord[] {
    return Array.from(this.assignees.values());
  }

  getById(id: string): AssigneeRecord | undefined {
    return this.assignees.get(id);
  }

  getByTask(taskId: string): AssigneeRecord[] {
    return this.all.filter((a) => a.taskId === taskId);
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
