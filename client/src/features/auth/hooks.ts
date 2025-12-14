// auth-hooks.ts
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Register, Me, Login, Logout, RefreshToken } from "./api";
import { ErrorResponse } from "@/types/responses/error-response";
import { LoginResponse, LogoutResponse, RegisterResponse } from "./type";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { authSessionManager } from "./auth-session-manager";
import { WORKSPACE_KEYS } from "../workspace/workspace-hooks";

export const AUTH_KEYS = {
  me: ["auth", "me"] as const,
} as const;

export function useRegister() {
  const router = useRouter();
  return useMutation({
    mutationFn: Register,
    onSuccess: (data: RegisterResponse) => {
      toast.success(
        data.message || "Account created successfully! Please sign in."
      );
      router.push("/authenthication");
    },
    onError: (error: ErrorResponse) => {
      toast.error(
        error.detail || error.title || "Registration failed unexpectedly."
      );
      console.error("Register Mutation Error:", error);
    },
  });
}

export function useLogin() {
  const router = useRouter();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: Login,
    onSuccess: async (data: LoginResponse) => {
      if (data.accessTokenExpiresAt && data.refreshTokenExpiresAt) {
        authSessionManager.setTokenExpiries(
          data.accessTokenExpiresAt,
          data.refreshTokenExpiresAt
        );
      }
      await queryClient.invalidateQueries({ queryKey: AUTH_KEYS.me });
      const user = await queryClient.fetchQuery({
        queryKey: AUTH_KEYS.me,
        queryFn: Me,
      });
      if (user) {
        toast.success(data.message || `Welcome back, ${user.name || "User"}!`);
        router.replace("/");
      } else {
        toast.error(
          "Login successful, but failed to retrieve user data. Please try again later."
        );
        router.replace("/");
      }
    },
    onError: (error: ErrorResponse) => {
      authSessionManager.clearSession();
      toast.error(
        error.detail ||
          error.title ||
          "Login failed. Please check your credentials."
      );
    },
  });
}

export function useLogout() {
  const router = useRouter();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: Logout,
    onMutate: () => {
      // Optimistically clear session and queries
      authSessionManager.clearSession();
      queryClient.removeQueries({ queryKey: AUTH_KEYS.me });
      queryClient.removeQueries({ queryKey: WORKSPACE_KEYS.sidebar() });
      
      // Cancel all ongoing queries
      return () => queryClient.cancelQueries();
    },
    onSuccess: (data: LogoutResponse) => {
      toast.success(data.message || "You have been successfully logged out.");
      router.push("/authenthication");
    },
    onError: (error: ErrorResponse, _, rollback) => {
      // Rollback optimistic update if needed
      if (rollback) rollback();
      
      toast.error(
        error.detail ||
          error.title ||
          "Logout failed on server, but you have been logged out locally."
      );
      router.push("/authenthication");
    },
    onSettled: () => {
      // Ensure all queries are reset
      queryClient.removeQueries();
    }
  });
}

export function useRefresh() {
  return useMutation({
    mutationFn: RefreshToken,
    onSuccess: (data) => {
      if (data.accessTokenExpiresAt && data.refreshTokenExpiresAt) {
        authSessionManager.setTokenExpiries(
          data.accessTokenExpiresAt,
          data.refreshTokenExpiresAt
        );
      }
    },
    onError: (error: ErrorResponse) => {
      authSessionManager.clearSession();
      console.error("Refresh Mutation Error:", error);
    },
  });
}

export function useUser() {
  // Use the useQuery hook as before, but without the direct 'onError' option
  const queryResult = useQuery({
    queryKey: AUTH_KEYS.me,
    queryFn: Me,
    staleTime: 5 * 60 * 1000,
    retry: false,
    refetchOnMount: true,
    refetchOnWindowFocus: true,
    refetchOnReconnect: true,
    // Removed: onError: ...
  });
  return queryResult;
}
