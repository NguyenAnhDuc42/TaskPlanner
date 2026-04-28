import { api } from "@/lib/api-client";
import { getCookie, deleteCookie } from "@/lib/cookie-utils";
import { queryClient } from "@/main";
import { authKeys } from "./api";

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
    const delay = expiryTime - this.REFRESH_BUFFER - now;

    if (delay <= 0) {
      this.checkAndRefresh();
    } else {
      this.timeoutId = window.setTimeout(() => this.checkAndRefresh(), delay);
    }
  }

  private async checkAndRefresh() {
    try {
      await api.post("/auth/refresh");
      queryClient.invalidateQueries({ queryKey: authKeys.me() });
      this.scheduleNextCheck();
    } catch (error) {
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
