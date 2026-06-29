import type { TaskRecord } from "@/types/projects";
import { makeAutoObservable, observable } from "mobx";

export class TaskStore {
  tasks = observable.map<string, TaskRecord>();
  constructor() {
    makeAutoObservable(this);
  }

  get all(): TaskRecord[] {
    return Array.from(this.tasks.values());
  }

  getById(id: string): TaskRecord | undefined {
    return this.tasks.get(id);
  }

  getBySpace(spaceId: string): TaskRecord[] {
    return this.all.filter((task) => task.spaceId === spaceId);
  }
  getByFolder(folderId: string): TaskRecord[] {
    return this.all.filter((task) => task.folderId === folderId);
  }

  getSubTask(parentTaskId: string): TaskRecord[] {
    return this.all.filter((task) => task.parentTaskId === parentTaskId);
  }

  getByStatus(statusId: string): TaskRecord[] {
    return this.all.filter((task) => task.statusId === statusId);
  }

  getFavorites(): TaskRecord[] {
    return this.all
      .filter((task) => task.isFavorite)
      .sort((a, b) =>
        (a.favoriteOrderKey ?? "").localeCompare(b.favoriteOrderKey ?? ""),
      );
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
      this.tasks.set(task.id, task);
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
