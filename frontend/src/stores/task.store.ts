import type { TaskRecord } from "@/types/projects";
import { makeAutoObservable, observable } from "mobx";

// Stable identity for empty lookups so observer re-renders and useMemo deps don't see a "new"
// array every call.
const EMPTY_TASKS: TaskRecord[] = [];

export class TaskStore {
  // deep: false — records replaced wholesale, never mutated in place. See DocumentBlockStore.
  tasks = observable.map<string, TaskRecord>({}, { deep: false });
  constructor() {
    makeAutoObservable(this);
  }

  get all(): TaskRecord[] {
    return Array.from(this.tasks.values());
  }

  // Computed group-by indexes (makeAutoObservable turns getters into cached computeds): one O(N)
  // pass shared by every caller, invalidated only when the map changes — instead of each sidebar
  // row / board / view re-scanning the whole store on every render. Returned arrays are shared —
  // callers must copy before mutating (e.g. [...arr].sort()).
  private get bySpaceIndex(): Map<string, TaskRecord[]> {
    const index = new Map<string, TaskRecord[]>();
    for (const task of this.tasks.values()) {
      if (!task.spaceId) continue;
      const list = index.get(task.spaceId);
      if (list) list.push(task);
      else index.set(task.spaceId, [task]);
    }
    return index;
  }

  private get byFolderIndex(): Map<string, TaskRecord[]> {
    const index = new Map<string, TaskRecord[]>();
    for (const task of this.tasks.values()) {
      if (!task.folderId) continue;
      const list = index.get(task.folderId);
      if (list) list.push(task);
      else index.set(task.folderId, [task]);
    }
    return index;
  }

  private get byParentIndex(): Map<string, TaskRecord[]> {
    const index = new Map<string, TaskRecord[]>();
    for (const task of this.tasks.values()) {
      if (!task.parentTaskId) continue;
      const list = index.get(task.parentTaskId);
      if (list) list.push(task);
      else index.set(task.parentTaskId, [task]);
    }
    return index;
  }

  getById(id: string): TaskRecord | undefined {
    return this.tasks.get(id);
  }

  getBySpace(spaceId: string): TaskRecord[] {
    return this.bySpaceIndex.get(spaceId) ?? EMPTY_TASKS;
  }
  getByFolder(folderId: string): TaskRecord[] {
    return this.byFolderIndex.get(folderId) ?? EMPTY_TASKS;
  }

  getSubTask(parentTaskId: string): TaskRecord[] {
    return this.byParentIndex.get(parentTaskId) ?? EMPTY_TASKS;
  }

  getByStatus(statusId: string): TaskRecord[] {
    return this.all.filter((task) => task.statusId === statusId);
  }

  add(task: TaskRecord): void {
    this.tasks.set(task.id, task);
  }

  update(id: string, changes: Partial<TaskRecord>): void {
    const existing = this.tasks.get(id);
    if (existing) {
      this.tasks.set(id, { ...existing, ...changes });
    }
  }

  upsert(task: TaskRecord): void {
    this.tasks.set(task.id, task);
  }

  remove(id: string): void {
    this.tasks.delete(id);
  }

  hydrate(tasks: TaskRecord[]): void {
    this.tasks.clear();
    for (const task of tasks) {
      this.tasks.set(task.id, task);
    }
  }
  upsertMany(tasks: TaskRecord[]): void {
    for (const task of tasks) {
      this.upsert(task);
    }
  }

  removeMany(ids: string[]): void {
    for (const id of ids) {
      this.tasks.delete(id);
    }
  }

  clear(): void {
    this.tasks.clear();
  }
}
