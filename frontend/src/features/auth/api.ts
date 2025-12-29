import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import type { User } from "./types";

export const authKeys = {
  all: ["auth"] as const,
  me: () => [...authKeys.all, "me"] as const,
};

export const userQueryOptions = {
  queryKey: authKeys.me(),
  queryFn: async () => {
    try {
      const { data } = await api.get<User>("/auth/me");
      return data;
    } catch (error) {
      return null; // 401/error means not logged in
    }
  },
};

export function useUser() {
  return useQuery<User>({
    queryKey: authKeys.me(),
    queryFn: async () => {
      const { data } = await api.get("/auth/me");
      return data;
    },
    retry: false,
    staleTime: 5 * 60 * 1000, // 5 min - user data doesn't change often
    gcTime: 10 * 60 * 1000, // 10 min cache
    refetchOnWindowFocus: false, // Don't refetch on tab focus
  });
}

export function useLogout() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async () => {
      await api.post("/auth/logout");
    },
    onSuccess: () => {
      queryClient.setQueryData(authKeys.me(), null);
      window.location.href = "/auth/sign-in";
    },
  });
}
