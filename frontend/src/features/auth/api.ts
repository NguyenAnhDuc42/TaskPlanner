import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/api-client";
import { getCookie } from "@/lib/get-cookie";
import type { User } from "./types";

export const authKeys = {
  all: ["auth"] as const,
  me: () => [...authKeys.all, "me"] as const,
};

export const userQueryOptions = {
  queryKey: authKeys.me(),
  queryFn: async () => {
    if (!getCookie("is_logged_in")) return null;

    try {
      const { data } = await api.get<User>("/auth/me");
      return data;
    } catch (error) {
      return null;
    }
  },
};

export function useUser() {
  return useQuery<User>({
    queryKey: authKeys.me(),
    queryFn: async () => {
      if (!getCookie("is_logged_in")) return null!;

      const { data } = await api.get("/auth/me");
      return data;
    },
    retry: false,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
    refetchOnWindowFocus: false,
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

export function useUpdateProfile() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (payload: { name?: string; email?: string }) => {
      const { data } = await api.put<User>("/auth/profile", payload);
      return data;
    },
    onSuccess: (updatedUser) => {
      queryClient.setQueryData(authKeys.me(), updatedUser);
    },
  });
}

export function useChangePassword() {
  return useMutation({
    mutationFn: async (payload: { currentPassword: string; newPassword: string }) => {
      await api.post("/auth/change-password", payload);
    },
  });
}
