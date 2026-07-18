export interface DocumentRecord {
  id: string;
  workspaceId?: string;
  spaceId: string;
  parentDocumentId?: string | null;
  name: string;
  orderKey?: string;
  icon?: string;
  color?: string;
  createdAt?: string;
}
