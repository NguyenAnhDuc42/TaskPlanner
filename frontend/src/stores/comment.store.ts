import { makeAutoObservable, observable } from "mobx";
import type { CommentRecord } from "@/types/projects/comment-record";

export class CommentStore {
  comments = observable.map<string, CommentRecord>();

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
