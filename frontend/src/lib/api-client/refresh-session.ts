import { api } from "./client";
import { apiEvents } from "./events";
import { deleteCookie } from "../cookie-utils";
import { isConnectivityError } from "../is-connectivity-error";

let isRefreshing = false;
let isRedirecting = false;
let failedQueue: Array<{ resolve: () => void; reject: (reason?: unknown) => void }> = [];

const processQueue = (error: unknown) => {
  failedQueue.forEach((prom) => (error ? prom.reject(error) : prom.resolve()));
  failedQueue = [];
};

export function isRedirectingToSignIn(): boolean {
  return isRedirecting;
}

export function refreshSession(): Promise<void> {
  if (isRefreshing) {
    return new Promise<void>((resolve, reject) => {
      failedQueue.push({ resolve, reject });
    });
  }

  isRefreshing = true;
  return api.post("/auth/refresh")
    .then(() => {
      apiEvents.onTokenRefreshed.forEach((cb) => cb());
      processQueue(null);
    })
    .catch((refreshError) => {
      processQueue(refreshError);
      if (!isConnectivityError(refreshError)) {
        deleteCookie("is_logged_in");
        deleteCookie("atexp");

        if (!globalThis.location.pathname.startsWith("/auth/")) {
          isRedirecting = true;
          globalThis.location.href = "/auth/sign-in";
        }
      }
      throw refreshError;
    })
    .finally(() => {
      isRefreshing = false;
    });
}
