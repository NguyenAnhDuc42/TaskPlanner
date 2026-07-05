import { useEffect, useState, useSyncExternalStore } from "react";
import { autorun } from "mobx";
import { api } from "@/lib/api-client";
import { getCookie } from "@/lib/cookie-utils";
import { useStore } from "@/stores/root.store";
import { currentUserStore } from "./current-user.store";
import type { User } from "./types";

// ─── Types ────────────────────────────────────────────────────────────────────

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
}

// ─── Hooks ────────────────────────────────────────────────────────────────────

export function useUser() {
  const isLoggedIn = !!getCookie("is_logged_in");

  useEffect(() => {
    if (isLoggedIn) currentUserStore.ensureLoaded();
  }, [isLoggedIn]);

  const data = useSyncExternalStore(
    (onStoreChange) => autorun(() => { void currentUserStore.data; onStoreChange(); }),
    () => currentUserStore.data,
  );
  const isFetching = useSyncExternalStore(
    (onStoreChange) => autorun(() => { void currentUserStore.isFetching; onStoreChange(); }),
    () => currentUserStore.isFetching,
  );
  const isLoading = useSyncExternalStore(
    (onStoreChange) => autorun(() => { void currentUserStore.isLoading; onStoreChange(); }),
    () => currentUserStore.isLoading,
  );

  return {
    data: isLoggedIn ? (data ?? undefined) : undefined,
    isLoading,
    isFetching,
    status: !isLoggedIn
      ? "error"
      : isLoading || isFetching
        ? "pending"
        : data
          ? "success"
          : ("error" as "error" | "pending" | "success"),
  };
}

export function useLogin() {
  const [isPending, setIsPending] = useState(false);
  return {
    mutate: async (values: LoginRequest) => {
      setIsPending(true);
      try {
        await api.post("/auth/login", values);
        await currentUserStore.refetch();
      } finally {
        setIsPending(false);
      }
    },
    isPending,
  };
}

export function useRegister() {
  const [isPending, setIsPending] = useState(false);
  return {
    mutate: async (values: RegisterRequest) => {
      setIsPending(true);
      try {
        await api.post("/auth/register", { ...values, userName: values.name });
      } finally {
        setIsPending(false);
      }
    },
    isPending,
  };
}

export function useLogout() {
  const rootStore = useStore();
  return {
    mutate: async () => {
      try {
        await api.post("/auth/logout");
      } finally {
        // Wipe local data regardless of whether the server call succeeded — logout is a
        // device-hygiene action the user expects to work even if the network request fails.
        currentUserStore.clear();
        await rootStore.clearAllLocalData();
        localStorage.removeItem("lastWorkspaceId");
        sessionStorage.removeItem("activeWorkspaceId");
        window.location.href = "/auth/sign-in";
      }
    },
  };
}

export function useForgotPassword() {
  const [isPending, setIsPending] = useState(false);
  return {
    mutate: async (email: string) => {
      setIsPending(true);
      try {
        await api.post("/auth/forgot-password", { email });
      } finally {
        setIsPending(false);
      }
    },
    isPending,
  };
}

export function useResetPassword() {
  const [isPending, setIsPending] = useState(false);
  return {
    mutate: async (token: string, newPassword: string) => {
      setIsPending(true);
      try {
        await api.post("/auth/reset-password", { token, newPassword });
      } finally {
        setIsPending(false);
      }
    },
    isPending,
  };
}

export function useUpdateProfile() {
  return {
    mutate: async (payload: { name?: string; email?: string }) => {
      const { data } = await api.put<User>("/auth/profile", payload);
      await currentUserStore.refetch();
      return data;
    },
  };
}

export function useChangePassword() {
  return {
    mutate: async (payload: { currentPassword: string; newPassword: string }) => {
      await api.post("/auth/change-password", payload);
    },
  };
}
