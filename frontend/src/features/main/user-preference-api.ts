import { useQuery, useMutation, useQueryClient, queryOptions } from "@tanstack/react-query";
import { api } from "@/lib/api-client";

// ─── Types ───────────────────────────────────────────────
export interface WorkspaceSetting {
  sideBarWidth?: number;
  mainContentWidth?: number;
  contextContentWidth?: number;
  isSidebarOpen: boolean;
}

export interface UserPreference {
  userId: string;
  theme: string;
  lastWorkspaceId: string | null;
  sidebarWidth: number;
  sidebarCollapsed: boolean;
  layoutData: string | null;
  workspaceSettings: Record<string, WorkspaceSetting>;
}

// ─── Query Keys ──────────────────────────────────────────
export const userPreferenceKeys = {
  all: ["user-preference"] as const,
  detail: () => [...userPreferenceKeys.all, "detail"] as const,
};

// ─── Query Options (for use in route loaders) ────────────
export const userPreferenceQueryOptions = queryOptions({
  queryKey: userPreferenceKeys.detail(),
  queryFn: async () => {
    const { data } = await api.get<UserPreference>("/users/preferences");
    return data;
  },
  staleTime: 1000 * 60 * 5,
});

// ─── Hooks ───────────────────────────────────────────────
export function useUserPreference() {
  return useQuery(userPreferenceQueryOptions);
}

export function useUpdateUserPreference() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: Partial<Omit<UserPreference, "userId">>) => {
      await api.put("/users/preferences", payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userPreferenceKeys.all });
    },
  });
}
