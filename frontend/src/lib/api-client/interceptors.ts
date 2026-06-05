import axios from "axios";
import { api } from "./client";
import { apiEvents } from "./events";
import { generateRequestHash } from "./idempotency";
import { ApiError } from "@/types/api-error";
import { deleteCookie, getCookie } from "../cookie-utils";
import { toast } from "sonner";
import { signalRService } from "../signalr-service";

let isRefreshing = false;
let isRedirecting = false;
let failedQueue: Array<{
  resolve: (value: unknown) => void;
  reject: (reason?: any) => void;
}> = [];

const processQueue = (error: any, token: any = null) => {
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
      }
    }

    const connectionId = signalRService.getConnectionId();
    if (connectionId) {
      config.headers["X-Connection-Id"] = connectionId;
    }

    const isMutating = ["post", "put", "delete", "patch"].includes(config.method?.toLowerCase() || "");
    if (isMutating && !config.headers["X-Idempotency-Key"]) {
      try {
        config.headers["X-Idempotency-Key"] = await generateRequestHash(
          config.method || "post",
          config.url || "",
          config.data,
          config.params
        );
      } catch {
        config.headers["X-Idempotency-Key"] = crypto.randomUUID();
      }
    }

    return config;
  });

  api.interceptors.response.use(
    (response) => response,
    async (error: any) => {
      const originalRequest = error.config;
      const isAuthRequest = originalRequest?.url?.includes("/auth/") && 
                           !originalRequest?.url?.includes("/auth/me");

      if (error.response?.status === 401 && !originalRequest._retry && !isAuthRequest) {
        if (isRefreshing) {
          return new Promise((resolve, reject) => {
            failedQueue.push({ resolve, reject });
          }).then(() => api(originalRequest)).catch((err) => { throw err; });
        }

        originalRequest._retry = true;
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
          
          throw new ApiError(error);
        } finally {
          isRefreshing = false;
        }
      }

      if (error.response?.status === 403 && originalRequest?.url?.includes("/workspaces/")) {
        toast.error("You do not have access to this workspace.");
        isRedirecting = true;
        globalThis.location.href = "/";
        throw new ApiError(error);
      }

      if (error.response?.status >= 500) {
        toast.error("A server error occurred. Please try again later.");
      }

      throw axios.isAxiosError(error) ? new ApiError(error) : error;
    }
  );
}
