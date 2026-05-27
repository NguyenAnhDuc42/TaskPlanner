import { workspaceApi } from "@/store/workspaceApi";

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

export const userPreferenceApi = workspaceApi.injectEndpoints({
  endpoints: (build) => ({
    getUserPreference: build.query<UserPreference, void>({
      query: () => ({ url: "/users/preferences", method: "GET" }),
      providesTags: ["UserPreference"],
    }),
    updateUserPreference: build.mutation<void, Partial<Omit<UserPreference, "userId">>>({
      query: (payload) => ({ url: "/users/preferences", method: "PUT", data: payload }),
      invalidatesTags: ["UserPreference"],
    }),
  }),
});

export const {
  useGetUserPreferenceQuery,
  useUpdateUserPreferenceMutation,
} = userPreferenceApi;

export function useUserPreference() {
  return useGetUserPreferenceQuery();
}

export function useUpdateUserPreference() {
  const [updatePreferenceTrigger] = useUpdateUserPreferenceMutation();
  return {
    mutate: (payload: Partial<Omit<UserPreference, "userId">>) => updatePreferenceTrigger(payload).unwrap(),
  };
}
