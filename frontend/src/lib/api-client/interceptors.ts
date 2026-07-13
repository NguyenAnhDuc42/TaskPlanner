import type { AxiosError, InternalAxiosRequestConfig } from "axios";
import axios from "axios";
import { api } from "./client";
import { apiEvents } from "./events";
import { ApiError, type ProblemDetails } from "@/types/api-error";
import { getCookie } from "../cookie-utils";
import { toast } from "sonner";
import { signalRService } from "../signalr-service";
import { refreshSession, isRedirectingToSignIn } from "./refresh-session";

let isRedirecting = false;

// Define an extended interface for retry logic
interface ExtendedAxiosRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

export function setupInterceptors() {
  api.interceptors.request.use(async (config) => {
    if (isRedirecting || isRedirectingToSignIn()) {
      throw new axios.Cancel("API request cancelled: redirecting user");
    }

    const isAuthRequest = config.url?.includes("/auth/") && !config.url?.includes("/auth/me");
    if (!isAuthRequest && !getCookie("is_logged_in")) {
      if (!globalThis.location.pathname.startsWith("/auth/")) {
        isRedirecting = true;
        globalThis.location.href = "/auth/sign-in";
      }
      throw new axios.Cancel("API request cancelled: no active session");
    }
    const connectionId = signalRService.getConnectionId();
    if (connectionId) {
      config.headers["X-Connection-Id"] = connectionId;
    }

 
    const isWorkspaceAgnostic =
      (config.url?.includes("/auth/") ?? false) ||
      (config.url?.includes("/notifications/") ?? false) ||
      config.url === "/workspaces/sync" ||
      config.url === "/workspaces/sync/join";
    if (!isWorkspaceAgnostic && !config.headers["X-Workspace-Id"]) {
      const activeWorkspaceId = sessionStorage.getItem("activeWorkspaceId");
      if (activeWorkspaceId) config.headers["X-Workspace-Id"] = activeWorkspaceId;
    }

    const isMutating = ["post", "put", "delete", "patch"].includes(config.method?.toLowerCase() || "");
    if (isMutating && !config.headers["X-Idempotency-Key"]) {
  
      config.headers["X-Idempotency-Key"] = crypto.randomUUID();
    }

    return config;
  });

  api.interceptors.response.use(
    (response) => response,
    async (error: unknown) => {
      const isAxiosErr = axios.isAxiosError(error);
      const originalRequest = (isAxiosErr ? error.config : undefined) as ExtendedAxiosRequestConfig | undefined;
      const isAuthRequest = originalRequest?.url?.includes("/auth/") && 
                           !originalRequest?.url?.includes("/auth/me");

      if (isAxiosErr && originalRequest && error.response?.status === 401 && !originalRequest._retry && !isAuthRequest) {
        originalRequest._retry = true;

        try {
          await refreshSession();
          return await api(originalRequest);
        } catch {
          throw isAxiosErr ? new ApiError(error as AxiosError<ProblemDetails>) : error;
        }
      }

      if (isAxiosErr && error.response?.status === 403) {
        const url = originalRequest?.url ?? "";
        const isWorkspaceRootAccess = /\/workspaces\/[a-f0-9-]+(\/me\/permissions)?\/?$/i.test(url);
        const problemData = error.response?.data as { code?: string } | undefined;

        if (problemData?.code === "workspace_access_denied") {
          const revokedWorkspaceId =
            (originalRequest?.headers?.["X-Workspace-Id"] as string | undefined) ??
            sessionStorage.getItem("activeWorkspaceId") ??
            undefined;
          if (revokedWorkspaceId) {
            apiEvents.onWorkspaceAccessRevoked.forEach((cb) => cb(revokedWorkspaceId));
          }
          toast.error("You no longer have access to this workspace.");
          throw new ApiError(error as AxiosError<ProblemDetails>);
        }

        if (isWorkspaceRootAccess) {
          toast.error("You do not have access to this workspace.");
          throw new ApiError(error as AxiosError<ProblemDetails>);
        }
        toast.error("You don't have permission to do that.");
        throw new ApiError(error as AxiosError<ProblemDetails>);
      }

      if (isAxiosErr && (error.response?.status ?? 0) >= 500) {
        toast.error("A server error occurred. Please try again later.");
      }

      // Safety net for everything else that got a real response from the server (400/404/409/422,
      // etc.) — previously these fell through with zero UI feedback unless the specific call site
      // happened to catch and toast it itself, which about half of them didn't. Requests with no
      // response at all (network drop/timeout) are deliberately excluded — those are handled by
      // each mutation's own isConnectivityError path (keep queued, retry), not a failure toast.
      if (isAxiosErr && error.response && !isAuthRequest && error.response.status !== 401 && error.response.status !== 403) {
        const data = error.response.data as ProblemDetails | undefined;
        toast.error(data?.detail || data?.title || "Something went wrong. Please try again.");
      }

      throw isAxiosErr ? new ApiError(error as AxiosError<ProblemDetails>) : error;
    }
  );
}
