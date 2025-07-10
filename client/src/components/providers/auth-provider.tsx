// src/components/providers/auth-provider.tsx
"use client";

import { createContext, useContext, useMemo, useEffect } from "react";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { AUTH_KEYS, useUser } from "@/features/auth/hooks";
import { User } from "@/types/user";

import { toast } from "sonner";
import { AxiosError } from "axios";
import { authSessionManager } from "@/features/auth/auth-session-manager";
import { RefreshToken } from "@/features/auth/api";

interface UserContextType {
  user: User | null;
  isLoading: boolean;
  isFetching: boolean;
  isError: boolean;
  refetch: () => void;
  isAuthenticated: boolean;
}

export const UserContext = createContext<UserContextType | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const queryClient = useQueryClient();

  const {
    data,
    isLoading,
    isError,
    refetch,
    isFetching,
    error: queryError,
  } = useUser();

  const user: User | null = data ?? null;
  const isAuthenticated = !!user;

  // Handle authentication errors
  useEffect(() => {
    if (isError) {
      if (
        queryError instanceof AxiosError &&
        (queryError.response?.status === 401 ||
          queryError.response === undefined)
      ) {
        console.error(
          "AuthProvider useEffect: Session invalid or network error after refresh attempt. Logging out locally."
        );
        authSessionManager.clearSession();
        queryClient.removeQueries({ queryKey: AUTH_KEYS.me });
        router.replace("/authenthication");
        toast.error("Your session has expired. Please log in again.");
      } else {
        console.error(
          "AuthProvider useEffect: Unexpected error fetching user.",
          queryError
        );
      }
    }
  }, [isError, queryError, queryClient, router]);
  useEffect(() => {
    if (
      isAuthenticated &&
      typeof window !== "undefined" &&
      !localStorage.getItem("accessTokenExpiresAt")
    ) {
      RefreshToken()
        .then((response) => {
          authSessionManager.setTokenExpiries(
            response.accessTokenExpiresAt,
            response.refreshTokenExpiresAt
          );
        })
        .catch((err) => {
          console.error("Silent refresh on boot failed:", err);
        });
    }
  }, [isAuthenticated]);

  const contextValue = useMemo(
    () => ({
      user,
      isLoading,
      isFetching,
      isError,
      refetch,
      isAuthenticated,
    }),
    [user, isLoading, isFetching, isError, refetch, isAuthenticated]
  );

  return (
    <UserContext.Provider value={contextValue}>{children}</UserContext.Provider>
  );
}

// Custom hook to consume the UserContext
export function useUserContext() {
  const ctx = useContext(UserContext);
  if (!ctx) {
    throw new Error("useUserContext must be used within an AuthProvider");
  }
  return ctx;
}
