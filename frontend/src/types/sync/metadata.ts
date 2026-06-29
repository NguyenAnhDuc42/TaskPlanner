export interface WorkspaceMetadata {
  workspaceId: string;
  firstSyncId: number;
  lastSyncId: number;
  databaseVersion: number;
  bootstrappedAt: number; //maybe be for lomg day fetch debug like after a month of not logging in
}
