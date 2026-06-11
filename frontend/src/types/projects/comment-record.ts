export interface CommentRecord {
  id: string;
  content: string;
  creatorId: string;
  taskId?: string;
  parentCommentId?: string;
  isEdited: boolean;
  createdAt: string;
  updatedAt?: string;
}
