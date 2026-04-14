import axios from "axios";
import { queryClient } from "@/main";
import { authKeys } from "@/features/auth/api";
import { ApiError } from "@/types/api-error";
import { toast } from "sonner";

export const api = axios.create({
  baseURL: "/api",
  withCredentials: true,

  headers: {
    "Content-Type": "application/json",
  },
});

// Request Queue for concurrent 401s
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value: unknown) => void;
  reject: (reason?: any) => void;
}> = [];

const processQueue = (error: any, token: any = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });

  failedQueue = [];
};

// Request Interceptor: Inject Workspace ID and other context
api.interceptors.request.use((config) => {
  // Automatically inject Workspace ID if we're in a workspace route
  if (!config.headers["X-Workspace-Id"]) {
    const workspaceIdMatch = window.location.pathname.match(
      /\/workspaces\/([a-f\d-]+)/i,
    );
    if (workspaceIdMatch) {
      config.headers["X-Workspace-Id"] = workspaceIdMatch[1];
    }
  }

  return config;
});

/**
 * Normalization & Specialized Global Handlers
 */
api.interceptors.response.use(
  (response) => response,
  async (error: any) => {
    const originalRequest = error.config;

    // --- 1. Reactive Refresh on 401 ---
    const isAuthAction =
      originalRequest?.url?.includes("/auth/") &&
      !originalRequest?.url?.includes("/auth/me");

    if (error.response?.status === 401 && !originalRequest._retry && !isAuthAction) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then(() => api(originalRequest))
          .catch((err) => Promise.reject(err));
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
        if (!window.location.pathname.includes("/auth/")) {
          window.location.href = "/auth/sign-in";
        }
        return Promise.reject(new ApiError(error));
      } finally {
        isRefreshing = false;
      }
    }

    // --- 2. Membership Access Denial (403 on Workspaces) ---
    if (
      error.response?.status === 403 &&
      originalRequest?.url?.includes("/workspaces/")
    ) {
      toast.error("You do not have access to this workspace.");
      window.location.href = "/";
      return Promise.reject(new ApiError(error));
    }

    // --- 3. Global Toast for Server Errors (500+) ---
    if (error.response?.status >= 500) {
      toast.error("A server error occurred. Please try again later.");
    }

    // --- 4. Normalize to ApiError ---
    if (axios.isAxiosError(error)) {
      return Promise.reject(new ApiError(error));
    }

    return Promise.reject(error);
  },
);
