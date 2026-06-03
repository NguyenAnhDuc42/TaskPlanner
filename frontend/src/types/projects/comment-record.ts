export interface CommentRecord {
  id: string;
  content: string;
  creatorId: string;
  projectTaskId?: string;
  parentCommentId?: string;
  isEdited: boolean;
  createdAt: string; // ISO 8601 string
  updatedAt?: string; // ISO 8601 string
}
