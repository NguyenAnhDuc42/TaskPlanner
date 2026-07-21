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
  // Whichever is more recent: the document's own metadata edit (rename/move/icon) or the newest
  // edit to any of its DocumentBlocks (actual page content) — see GetBootstrapHandler.
  lastEditedAt?: string;
}
