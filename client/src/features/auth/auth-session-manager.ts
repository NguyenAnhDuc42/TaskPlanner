import { QueryClient } from "@tanstack/react-query";
import { RefreshToken } from "./api";
import { AUTH_KEYS } from "./hooks";
import { AxiosError } from "axios";
import { toast } from "sonner";

class AuthSessionManager {
  private queryClient: QueryClient | null = null;
  private refreshTimer: NodeJS.Timeout | null = null;
  private accessTokenExpiresAt: Date | null = null;
  private refreshTokenExpiresAt: Date | null = null;

  constructor() {
    if (typeof window !== "undefined") {
      const storedAccessExpiry = localStorage.getItem("accessTokenExpiresAt");
      const storedRefreshExpiry = localStorage.getItem("refreshTokenExpiresAt");
      if (storedAccessExpiry)
        this.accessTokenExpiresAt = new Date(storedAccessExpiry);
      if (storedRefreshExpiry)
        this.refreshTokenExpiresAt = new Date(storedRefreshExpiry);

      if (
        this.accessTokenExpiresAt &&
        this.accessTokenExpiresAt.getTime() > Date.now()
      ) {
        this.scheduleProactiveRefresh();
      }
    }
  }

  setQueryClient(client: QueryClient) {
    this.queryClient = client;
  }

  setTokenExpiries(accessExpiry: string, refreshExpiry: string) {
    this.accessTokenExpiresAt = new Date(accessExpiry);
    this.refreshTokenExpiresAt = new Date(refreshExpiry);

    if (typeof window !== "undefined") {
      localStorage.setItem(
        "accessTokenExpiresAt",
        this.accessTokenExpiresAt.toISOString()
      );
      localStorage.setItem(
        "refreshTokenExpiresAt",
        this.refreshTokenExpiresAt.toISOString()
      );
    }

    this.scheduleProactiveRefresh();
    console.log(
      `AuthSessionManager: Token expiries updated. Access: ${this.accessTokenExpiresAt.toLocaleString()}, Refresh: ${this.refreshTokenExpiresAt.toLocaleString()}`
    );
  }

  clearSession() {
    this.stopProactiveRefresh();
    this.accessTokenExpiresAt = null;
    this.refreshTokenExpiresAt = null;
    if (typeof window !== "undefined") {
      localStorage.removeItem("accessTokenExpiresAt");
      localStorage.removeItem("refreshTokenExpiresAt");
    }
    console.log("AuthSessionManager: Session cleared.");
  }
  private scheduleProactiveRefresh() {
    this.stopProactiveRefresh(); // Clear any existing timer to prevent duplicates

    if (!this.accessTokenExpiresAt) {
      console.warn(
        "AuthSessionManager: No access token expiry to schedule proactive refresh."
      );
      return;
    }

    const now = Date.now();
    const accessTokenRemainingMs = this.accessTokenExpiresAt.getTime() - now;

    // Calculate refresh time: target 60% through remaining access token life, but at least 1 minute before expiry.
    // This gives ample time to refresh before the token actually expires.
    const refreshTriggerMs = accessTokenRemainingMs * 0.6; // Refresh when 60% of time passed
    const minRefreshBeforeExpiryMs = 60 * 1000; // Minimum 1 minute before expiry

    let delayMs = Math.max(refreshTriggerMs, minRefreshBeforeExpiryMs);

    // If access token is very short-lived (e.g., < 2 minutes), refresh sooner
    if (accessTokenRemainingMs > 0 && accessTokenRemainingMs < 2 * 60 * 1000) {
      delayMs = accessTokenRemainingMs / 2; // Refresh halfway through very short life
    }

    // Ensure delay is positive and within the valid lifetime
    if (delayMs <= 0 || delayMs >= accessTokenRemainingMs) {
      console.warn(
        `AuthSessionManager: Access token already expired or very close (${
          accessTokenRemainingMs / 1000
        }s remaining). Not scheduling proactive refresh.`
      );
      return; // Let the Axios interceptor handle reactive refresh
    }

    console.log(
      `AuthSessionManager: Scheduling proactive refresh in ${Math.round(
        delayMs / 1000
      )} seconds.`
    );
    this.refreshTimer = setTimeout(this.performProactiveRefresh, delayMs);
  }

  private stopProactiveRefresh() {
    if (this.refreshTimer) {
      clearTimeout(this.refreshTimer);
      this.refreshTimer = null;
      console.log("AuthSessionManager: Proactive refresh timer stopped.");
    }
  }

  private performProactiveRefresh = async () => {
    if (!this.queryClient) {
      console.error(
        "AuthSessionManager: QueryClient not set, cannot perform proactive refresh."
      );
      return;
    }

    console.log("AuthSessionManager: Executing proactive token refresh...");
    try {
      // Call the refresh API. The backend will set new cookies and return new expiry dates.
      const response = await RefreshToken();
      // Update expiry times and reschedule the next proactive refresh
      this.setTokenExpiries(
        response.accessTokenExpiresAt,
        response.refreshTokenExpiresAt
      );
      console.log(
        "AuthSessionManager: Proactive refresh successful, new timer scheduled."
      );

      // Invalidate the 'me' query to ensure any useUser hooks get fresh data if needed.
      // This is generally good practice to keep the client-side user state synchronized.
      await this.queryClient.invalidateQueries({ queryKey: AUTH_KEYS.me });
    } catch (error: unknown) {
      console.error("AuthSessionManager: Proactive refresh failed.", error);
      this.stopProactiveRefresh(); // Stop the timer if refresh fails

      // If refresh token has expired or is invalid (e.g., 401/400 from /auth/refresh)
      if (
        error instanceof AxiosError &&
        (error.response?.status === 401 || error.response?.status === 400)
      ) {
        toast.error("Your session has expired. Please log in again.");
        this.queryClient.removeQueries({ queryKey: AUTH_KEYS.me }); // Clear user from cache
        // Note: Direct router redirection here is tricky. Rely on useUser's onError for this.
      } else {
        toast.error("Failed to renew session. Please try again later.");
      }
    }
  };
}

// Export a singleton instance to be used throughout the application
export const authSessionManager = new AuthSessionManager();
