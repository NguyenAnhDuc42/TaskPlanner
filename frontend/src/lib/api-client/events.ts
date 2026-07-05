export const apiEvents = {
  onTokenRefreshed: [] as (() => void)[],
  onWorkspaceAccessRevoked: [] as ((workspaceId: string) => void)[],

  onLogout: [] as (() => Promise<void> | void)[],
};
