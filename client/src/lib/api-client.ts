import { RefreshToken } from "@/features/auth/api";
import { ErrorResponse } from "@/types/responses/error-response";
import { AxiosError, AxiosResponse, InternalAxiosRequestConfig } from "axios";
import axios from "axios";

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
          const refreshResponse = await RefreshToken();
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
          return apiClient(originalRequest);
        } catch (refreshError: unknown) {
          isRefreshing = false;
          if (refreshError instanceof AxiosError || refreshError instanceof Error) {
            processQueue(refreshError);
          } else {
            processQueue(new Error("Unknown refresh error occurred."));
          }
          console.error("Axios Interceptor: Reactive token refresh failed (RFT likely expired or invalid).", refreshError);
        }
      } else {
        return new Promise<AxiosResponse<unknown>>((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
        .then(() => apiClient(originalRequest))
        .catch(err => Promise.reject(err));
      }
    }

    const apiError = error.response?.data;

    if (apiError && typeof apiError.status === 'number') {
        throw apiError;
    } else {
        throw {
            type: 'about:blank',
            title: 'Network Error',
            status: error.response?.status || 500,
            detail: error.message,
            instance: undefined,
            errors: {}
        } as ErrorResponse;
    }
  }
);

export default apiClient;