import axios from "axios";
import { queryClient } from "@/main";
import { authKeys } from "@/features/auth/api";
import { ApiError } from "@/types/api-error";
import { deleteCookie } from "./cookie-utils";
import { toast } from "sonner";

export const api = axios.create({
  baseURL: "/api",
  withCredentials: true,
  headers: {
    "Content-Type": "application/json",
  },
});

let isRefreshing = false;
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

// Request Interceptor: Inject Workspace ID
api.interceptors.request.use((config) => {
  if (!config.headers["X-Workspace-Id"]) {
    const workspaceIdMatch = window.location.pathname.match(/\/workspaces\/([a-f\d-]+)/i);
    if (workspaceIdMatch) {
      config.headers["X-Workspace-Id"] = workspaceIdMatch[1];
    }
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
        queryClient.invalidateQueries({ queryKey: authKeys.me() });
        processQueue(null);
        return api(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError);
        
        // Fatal Refresh Error: The session is completely dead.
        // The server already cleared our cookies via Set-Cookie, 
        // but we clear them in JS too just to be absolute.
        deleteCookie("is_logged_in");
        deleteCookie("atexp");

        // Redirect to sign-in if we aren't already there
        if (!window.location.pathname.startsWith("/auth/")) {
            window.location.href = "/auth/sign-in";
        }
        
        return Promise.reject(new ApiError(error));
      } finally {
        isRefreshing = false;
      }
    }

    if (error.response?.status === 403 && originalRequest?.url?.includes("/workspaces/")) {
      toast.error("You do not have access to this workspace.");
      window.location.href = "/";
      return Promise.reject(new ApiError(error));
    }

    if (error.response?.status >= 500) {
      toast.error("A server error occurred. Please try again later.");
    }

    return Promise.reject(axios.isAxiosError(error) ? new ApiError(error) : error);
  }
);
