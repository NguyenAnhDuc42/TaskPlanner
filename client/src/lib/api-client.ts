import { ErrorResponse } from "@/types/responses/error-response";
import { AxiosError, AxiosResponse, InternalAxiosRequestConfig } from "axios";
import axios from "axios";
import { useWorkspaceStore } from "@/utils/workspace-store";
import type { RefreshTokenResponse } from "@/features/auth/type";

interface CustomAxiosRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

interface QueuedRequestPromise {
  resolve: (value: AxiosResponse<unknown>) => void;
  reject: (reason?: AxiosError<ErrorResponse> | Error) => void;
}

let isRefreshing = false;
let failedQueue: QueuedRequestPromise[] = [];

const processQueue = (error: AxiosError<ErrorResponse> | Error | null) => {
  failedQueue.forEach(prom => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve({} as AxiosResponse<unknown>);
    }
  });
  failedQueue = [];
};

const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5198/api",
  withCredentials: true,
  headers: {
    "Content-Type": "application/json",
  },
});
apiClient.interceptors.request.use((config) => {
  const workspaceId = useWorkspaceStore.getState().selectedWorkspaceId;
  if (workspaceId) {
    config.headers['X-Workspace-Id'] = workspaceId;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  async (error: AxiosError<ErrorResponse>) => {
    const originalRequest = error.config as CustomAxiosRequestConfig;

    if (error.response?.status === 401 && originalRequest?.url !== '/auth/refresh' && !originalRequest?._retry) {
      originalRequest._retry = true; 
      if (!isRefreshing) {
        isRefreshing = true;
        console.log("Axios Interceptor: Access token expired, attempting reactive refresh.");
        try {
          // Direct axios call to prevent circular dependency with auth/api.ts
          const { data: refreshResponse } = await axios.post<RefreshTokenResponse>(
            `${apiClient.defaults.baseURL}/auth/refresh`,
            {},
            { withCredentials: true }
          );
          console.log("Axios Interceptor: Reactive token refresh successful.");

          if (refreshResponse?.accessTokenExpiresAt && refreshResponse?.refreshTokenExpiresAt) {
            const { authSessionManager } = await import("@/features/auth/auth-session-manager");
            authSessionManager.setTokenExpiries(
              refreshResponse.accessTokenExpiresAt,
              refreshResponse.refreshTokenExpiresAt
            );
          }

          isRefreshing = false;
          processQueue(null); 
          // Retry the original request, which will now use the new auth cookie
          return apiClient(originalRequest);
        } catch (refreshError: unknown) {
          isRefreshing = false;
          if (refreshError instanceof AxiosError || refreshError instanceof Error) {
            processQueue(refreshError);
          } else {
            processQueue(new Error("Unknown refresh error occurred."));
          }
          console.error("Axios Interceptor: Reactive token refresh failed (RFT likely expired or invalid).", refreshError);
          
          // If refresh fails, clear session and redirect to login
          const { authSessionManager } = await import("@/features/auth/auth-session-manager");
          authSessionManager.clearSession();
          if (typeof window !== 'undefined') {
            window.location.href = '/authenthication';
          }
        }
      } else {
        return new Promise<AxiosResponse<unknown>>((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
        .then(() => apiClient(originalRequest))
        .catch(err => Promise.reject(err));
      }
    }

    // For all other errors, it's best to propagate the original AxiosError.
    // This preserves the full error context (status, headers, config) for the caller (e.g., TanStack Query's onError).
    return Promise.reject(error);
  }
);

export default apiClient;