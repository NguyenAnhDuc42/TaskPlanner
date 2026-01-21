export const membersKeys = {
    all: ["members"] as const,
    list : (workspaceId: string) => [...membersKeys.all, "list", workspaceId] as const,
};
