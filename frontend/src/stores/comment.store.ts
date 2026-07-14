import { makeAutoObservable, observable } from "mobx";
import type { CommentRecord } from "@/types/projects/comment-record";

export class CommentStore {
  // deep: false — records replaced wholesale, never mutated in place. See DocumentBlockStore.
  comments = observable.map<string, CommentRecord>({}, { deep: false });

  constructor() {
    makeAutoObservable(this);
  }

  get all(): CommentRecord[] {
    return Array.from(this.comments.values());
  }

  getById(id: string): CommentRecord | undefined {
    return this.comments.get(id);
  }

  getByTask(taskId: string): CommentRecord[] {
    return this.all.filter((c) => c.taskId === taskId).sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
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
