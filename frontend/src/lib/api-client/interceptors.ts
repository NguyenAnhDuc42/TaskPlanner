import axios from "axios";
import { api } from "./client";
import { apiEvents } from "./events";
import { ApiError } from "@/types/api-error";
import { deleteCookie, getCookie } from "../cookie-utils";
import { toast } from "sonner";
import { signalRService } from "../signalr-service";

let isRefreshing = false;
let isRedirecting = false;
let failedQueue: Array<{
  resolve: (value: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: unknown = null) => {
  failedQueue.forEach((prom) => {
    if (error) prom.reject(error);
    else prom.resolve(token);
  });
  failedQueue = [];
};

export function setupInterceptors() {
  api.interceptors.request.use(async (config) => {
    if (isRedirecting) {
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

    if (!config.headers["X-Workspace-Id"]) {
      const workspaceIdMatch = new RegExp(/\/workspaces\/([a-f\d-]+)/i).exec(globalThis.location.href);
      if (workspaceIdMatch) {
        config.headers["X-Workspace-Id"] = workspaceIdMatch[1];
      } else {
        // Fallback: loader sets this before API calls fire during navigation
        const stored = sessionStorage.getItem("activeWorkspaceId");
        if (stored) config.headers["X-Workspace-Id"] = stored;
      }
    }

    const connectionId = signalRService.getConnectionId();
    if (connectionId) {
      config.headers["X-Connection-Id"] = connectionId;
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
      const originalRequest = isAxiosErr ? error.config : undefined;
      const isAuthRequest = originalRequest?.url?.includes("/auth/") && 
                           !originalRequest?.url?.includes("/auth/me");

      if (isAxiosErr && originalRequest && error.response?.status === 401 && !(originalRequest as any)?._retry && !isAuthRequest) {
        if (isRefreshing) {
          return new Promise((resolve, reject) => {
            failedQueue.push({ resolve, reject });
          }).then(() => api(originalRequest)).catch((err) => { throw err; });
        }

        (originalRequest as any)._retry = true;
        isRefreshing = true;

        try {
          await api.post("/auth/refresh");
          apiEvents.onTokenRefreshed.forEach(cb => cb());
          processQueue(null);
          return await api(originalRequest);
        } catch (refreshError) {
          processQueue(refreshError);
          deleteCookie("is_logged_in");
          deleteCookie("atexp");

          if (!globalThis.location.pathname.startsWith("/auth/")) {
            isRedirecting = true;
            globalThis.location.href = "/auth/sign-in";
          }
          
          throw isAxiosErr ? new ApiError(error as any) : error;
        } finally {
          isRefreshing = false;
        }
      }

      if (isAxiosErr && error.response?.status === 403) {
        const url = originalRequest?.url ?? "";
        // Only redirect to home when the user is being denied access to the workspace itself.
        // Sub-resource operations (pin, members, spaces, etc.) should just show a toast.
        const isWorkspaceRootAccess = /\/workspaces\/[a-f0-9-]+(\/me\/permissions)?\/?$/i.test(url);
        if (isWorkspaceRootAccess) {
          toast.error("You do not have access to this workspace.");
          isRedirecting = true;
          globalThis.location.href = "/";
          throw new ApiError(error as any);
        }
        toast.error("You don't have permission to do that.");
        throw new ApiError(error as any);
      }

      if (isAxiosErr && (error.response?.status ?? 0) >= 500) {
        toast.error("A server error occurred. Please try again later.");
      }

      throw isAxiosErr ? new ApiError(error as any) : error;
    }
  );
}
