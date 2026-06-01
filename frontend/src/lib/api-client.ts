import axios from "axios";

// Simple event channel to notify query layers when tokens refresh without circular dependencies
export const apiEvents = {
  onTokenRefreshed: [] as (() => void)[],
};

import { ApiError } from "@/types/api-error";
import { deleteCookie, getCookie } from "./cookie-utils";
import { toast } from "sonner";
import { signalRService } from "./signalr-service";

export const api = axios.create({
  baseURL: "/api",
  withCredentials: true,
  headers: {
    "Content-Type": "application/json",
  },
});

let isRefreshing = false;
let isRedirecting = false; // Protects against concurrent API storms during redirect
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

// Request Interceptor: Inject Workspace ID, SignalR Connection ID and prevent concurrent/unauthorized spam
api.interceptors.request.use((config) => {
  // 1. Drop requests immediately if we are already in the process of redirecting to login/home
  if (isRedirecting) {
    return Promise.reject(new axios.Cancel("API request cancelled: redirecting user"));
  }

  // 2. Drop requests immediately if session cookie is missing (except login/signup endpoints)
  const isAuthRequest = config.url?.includes("/auth/") && !config.url?.includes("/auth/me");
  if (!isAuthRequest && !getCookie("is_logged_in")) {
    if (!window.location.pathname.startsWith("/auth/")) {
      isRedirecting = true; // Lock outgoing requests
      window.location.href = "/auth/sign-in";
    }
    return Promise.reject(new axios.Cancel("API request cancelled: no active session"));
  }

  if (!config.headers["X-Workspace-Id"]) {
    const workspaceIdMatch = window.location.href.match(/\/workspaces\/([a-f\d-]+)/i);
    if (workspaceIdMatch) {
      config.headers["X-Workspace-Id"] = workspaceIdMatch[1];
    }
  }

  const connectionId = signalRService.getConnectionId();
  if (connectionId) {
    config.headers["X-Connection-Id"] = connectionId;
  }

  return config;
});

// Response Interceptor: Handle Refresh & Errors
api.interceptors.response.use(
  (response) => response,
  async (error: any) => {
    const originalRequest = error.config;

    // 1. Reactive Refresh on 401 (Unauthorized)
    // We don't refresh if the request was ALREADY an auth action (login/logout/refresh)
    const isAuthRequest = originalRequest?.url?.includes("/auth/") && 
                         !originalRequest?.url?.includes("/auth/me");

    if (error.response?.status === 401 && !originalRequest._retry && !isAuthRequest) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then(() => api(originalRequest)).catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        await api.post("/auth/refresh");
        apiEvents.onTokenRefreshed.forEach(cb => cb());
        processQueue(null);
        return api(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError);
        
        // Fatal Refresh Error: The session is completely dead.
        deleteCookie("is_logged_in");
        deleteCookie("atexp");

        // Redirect to sign-in if we aren't already there
        if (!window.location.pathname.startsWith("/auth/")) {
            isRedirecting = true; // Lock outgoing requests
            window.location.href = "/auth/sign-in";
        }
        
        return Promise.reject(new ApiError(error));
      } finally {
        isRefreshing = false;
      }
    }

    if (error.response?.status === 403 && originalRequest?.url?.includes("/workspaces/")) {
      toast.error("You do not have access to this workspace.");
      isRedirecting = true; // Lock outgoing requests
      window.location.href = "/";
      return Promise.reject(new ApiError(error));
    }

    if (error.response?.status >= 500) {
      toast.error("A server error occurred. Please try again later.");
    }

    return Promise.reject(axios.isAxiosError(error) ? new ApiError(error) : error);
  }
);
