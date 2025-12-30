import axios from "axios";
import { getCookie } from "./get-cookie";

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

// Request Interceptor: Proactive Refresh
api.interceptors.request.use(async (config) => {
  const isAuthAction = config.url?.includes("/auth/") && !config.url?.includes("/auth/me");

  if (isAuthAction) return config;

  const atexp = getCookie("atexp");
  if (atexp) {
    const expiryTime = Number(atexp) * 1000;
    const now = Date.now();

    // If within 1 minute of expiring, pause and refresh
    if (expiryTime - now < 60 * 1000) {
      if (!isRefreshing) {
        isRefreshing = true;
        try {
          await api.post("/auth/refresh");
          processQueue(null);
        } catch (error) {
          processQueue(error);
        } finally {
          isRefreshing = false;
        }
      } else {
        // Just wait for the active refresh
        await new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        });
      }
    }
  }

  return config;
});

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
        processQueue(null);
        return api(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError);
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
