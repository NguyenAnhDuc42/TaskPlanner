import type { TaskRecord } from "@/types/projects";
import { makeAutoObservable, observable, computed } from "mobx";

const EMPTY_TASKS: TaskRecord[] = [];

const byOrderKey = (a: TaskRecord, b: TaskRecord) => {
  const ka = a.orderKey ?? "";
  const kb = b.orderKey ?? "";
  if (ka !== kb) return ka < kb ? -1 : 1;
  return a.id < b.id ? -1 : a.id > b.id ? 1 : 0;
};

export class TaskStore {
  tasks = observable.map<string, TaskRecord>({}, { deep: false });
  constructor() {
    // keepAlive: these group-by indexes are read from plain (non-observer) call sites too —
    // mutation classes, the sync delta-handler, DnD handlers — where a plain MobX computed
    // provides no caching at all (it only memoizes while an active observer, e.g. a rendering
    // React component, is watching it; otherwise every read recomputes from scratch). keepAlive
    // pins them so they stay cached and incrementally-invalidated regardless of who's reading.
    makeAutoObservable<TaskStore, "bySpaceIndex" | "byFolderIndex" | "byParentIndex">(this, {
      bySpaceIndex: computed({ keepAlive: true }),
      byFolderIndex: computed({ keepAlive: true }),
      byParentIndex: computed({ keepAlive: true }),
    });
  }

  get all(): TaskRecord[] {
    return Array.from(this.tasks.values());
  }

  private get bySpaceIndex(): Map<string, TaskRecord[]> {
    const index = new Map<string, TaskRecord[]>();
    for (const task of this.tasks.values()) {
      if (!task.spaceId) continue;
      const list = index.get(task.spaceId);
      if (list) list.push(task);
      else index.set(task.spaceId, [task]);
    }
    for (const list of index.values()) list.sort(byOrderKey);
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
    for (const list of index.values()) list.sort(byOrderKey);
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
    for (const list of index.values()) list.sort(byOrderKey);
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
