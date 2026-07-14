import { makeAutoObservable, observable } from "mobx";
import type { CommentRecord } from "@/types/projects/comment-record";

const EMPTY_COMMENTS: CommentRecord[] = [];

export class CommentStore {
  // deep: false — records replaced wholesale, never mutated in place. See DocumentBlockStore.
  comments = observable.map<string, CommentRecord>({}, { deep: false });

  constructor() {
    makeAutoObservable(this);
  }

  get all(): CommentRecord[] {
    return Array.from(this.comments.values());
  }

  // Computed index — cached until the map changes; returned arrays are shared. See TaskStore.
  private get byTaskIndex(): Map<string, CommentRecord[]> {
    const index = new Map<string, CommentRecord[]>();
    for (const comment of this.comments.values()) {
      if (!comment.taskId) continue;
      const list = index.get(comment.taskId);
      if (list) list.push(comment);
      else index.set(comment.taskId, [comment]);
    }
    for (const list of index.values()) {
      list.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
    }
    return index;
  }

  getById(id: string): CommentRecord | undefined {
    return this.comments.get(id);
  }

  getByTask(taskId: string): CommentRecord[] {
    return this.byTaskIndex.get(taskId) ?? EMPTY_COMMENTS;
  }

  upsert(comment: CommentRecord): void {
    this.comments.set(comment.id, comment);
  }

  // Single MobX action = single transaction — see DocumentBlockStore.upsertMany.
  upsertMany(comments: CommentRecord[]): void {
    for (const c of comments) {
      this.comments.set(c.id, c);
    }
  }

  remove(id: string): void {
    this.comments.delete(id);
  }

  hydrate(comments: CommentRecord[]): void {
    this.comments.clear();
    for (const c of comments) {
      this.comments.set(c.id, c);
    }
  }

  clear(): void {
    this.comments.clear();
  }
}
