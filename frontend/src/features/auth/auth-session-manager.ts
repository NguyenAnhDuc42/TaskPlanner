import { api, apiEvents } from "@/lib/api-client";
import { getCookie, deleteCookie } from "@/lib/cookie-utils";

class AuthSessionManager {
  private timeoutId: number | null = null;
  private readonly REFRESH_BUFFER = 1000 * 60; // 1 minute before expiry

  public start() {
    this.scheduleNextCheck();
  }

  public stop() {
    if (this.timeoutId) {
      window.clearTimeout(this.timeoutId);
      this.timeoutId = null;
    }
  }

  private scheduleNextCheck() {
    this.stop();

    const atexp = getCookie("atexp");
    const isLoggedIn = getCookie("is_logged_in");

    if (!isLoggedIn || !atexp) return;

    const expiryTime = Number(atexp) * 1000;
    const now = Date.now();
    // If the local clock is way ahead, delay could be negative forever. 
    // Enforce a minimum 5 second delay to prevent rapid-fire infinite loops.
    const delay = Math.max(expiryTime - this.REFRESH_BUFFER - now, 5000);

    this.timeoutId = window.setTimeout(() => this.checkAndRefresh(), delay);
  }

  private async checkAndRefresh() {
    try {
      await api.post("/auth/refresh");
      apiEvents.onTokenRefreshed.forEach(cb => cb());
      this.scheduleNextCheck();
    } catch (error) {
      console.error("Token refresh failed", error);
      this.stop();
      deleteCookie("is_logged_in");
      deleteCookie("atexp");
      
      if (!window.location.pathname.startsWith("/auth/")) {
        window.location.href = "/auth/sign-in";
      }
    }
  }
}

export const sessionManager = new AuthSessionManager();
