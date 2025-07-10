import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Register, Me, Login, Logout, RefreshToken } from "./api";
import { ErrorResponse } from "@/types/responses/error-response";
import { LoginResponse, LogoutResponse, RegisterResponse } from "./type";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { authSessionManager } from "./auth-session-manager";

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
      console.log(error);
    },
  });
}
export function useLogin() {
  const router = useRouter();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: Login,
    onSuccess: async (data: LoginResponse) => {
      console.log("useLogin onSuccess called. Data:", data);
      if (data.accessTokenExpiresAt && data.refreshTokenExpiresAt) {
        console.log(
          "Calling setTokenExpiries with:",
          data.accessTokenExpiresAt,
          data.refreshTokenExpiresAt
        );
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
          "Login successful, but failed to retrieve user data. Please try trying again later."
        );
        router.replace("/");
      }
    },
    onError: (error: ErrorResponse) => {
      authSessionManager.clearSession(); // Clear session manager state on login failure
      toast.error(
        error.detail ||
          error.title ||
          "Login failed. Please check your credentials."
      );
      console.error("Login Mutation Error:", JSON.stringify(error, null, 2));
    },
  });
}

export function useLogout() {
  const router = useRouter();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: Logout,
    onSuccess: (data: LogoutResponse) => {
      authSessionManager.clearSession();
      queryClient.removeQueries({ queryKey: AUTH_KEYS.me });
      toast.success(data.message || "You have been successfully logged out.");
      router.push("/authenthication");
    },
    onError: (error: ErrorResponse) => {
      authSessionManager.clearSession(); // Clear session manager state
      queryClient.removeQueries({ queryKey: AUTH_KEYS.me });
      toast.error(
        error.detail ||
          error.title ||
          "Logout failed on server, but you have been logged out locally."
      );
      router.replace("/authenthication");
      console.error("Logout Mutation Error:", error);
    },
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

  // Add a useEffect to watch for errors and perform logout/redirection

  return queryResult;
}
