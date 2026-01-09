import axios from "axios";
import { queryClient } from "@/main";
import { authKeys } from "@/features/auth/api";

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

// Request Interceptor: Simply return config
api.interceptors.request.use((config) => config);

// Response Interceptor: Reactive Refresh on 401
api.interceptors.response.use(
  (response) => response,
  async (error: any) => {
    const originalRequest = error.config;

    // Skip refresh for auth guest/action failures
    const isAuthAction =
      originalRequest?.url?.includes("/auth/") &&
      !originalRequest?.url?.includes("/auth/me");

    if (isAuthAction) {
      return Promise.reject(error);
    }

    if (error.response?.status === 401 && !originalRequest._retry) {
      console.log("[API] 401 detected on:", originalRequest.url);
      if (isRefreshing) {
        console.log("[API] Already refreshing, queuing request.");
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then(() => api(originalRequest))
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        console.log("[API] Attempting reactive refresh...");
        await api.post("/auth/refresh");
        queryClient.invalidateQueries({ queryKey: authKeys.me() });
        processQueue(null);
        console.log("[API] Refresh successful, retrying original request.");
        return api(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError);
        console.error("[API] Reactive refresh failed, redirecting to sign-in.");
        if (!window.location.pathname.includes("/auth/")) {
          window.location.href = "/auth/sign-in";
        }
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
