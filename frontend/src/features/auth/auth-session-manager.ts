import { api } from "@/lib/api-client";
import { getCookie } from "@/lib/get-cookie";
import { queryClient } from "@/main";
import { authKeys } from "./api";

class AuthSessionManager {
  private timeoutId: number | null = null;
  private readonly REFRESH_BUFFER = 1000 * 60; // 1 minute before expiry

  public start() {
    console.log("[AuthManager] Starting session manager...");
    this.scheduleNextCheck();
  }

  public stop() {
    if (this.timeoutId) {
      console.log("[AuthManager] Stopping session manager.");
      window.clearTimeout(this.timeoutId);
      this.timeoutId = null;
    }
  }

  private scheduleNextCheck() {
    this.stop();

    const isLoggedIn = getCookie("is_logged_in");
    const atexp = getCookie("atexp");

    console.log(
      "[AuthManager] Scheduling check. is_logged_in:",
      !!isLoggedIn,
      "atexp:",
      atexp
    );

    if (!isLoggedIn) {
      console.log("[AuthManager] Not logged in, skipping schedule.");
      return;
    }

    if (!atexp) {
      console.log(
        "[AuthManager] No expiry found. Refreshing immediately to recover cookies."
      );
      this.checkAndRefresh();
      return;
    }

    const expiryTime = Number(atexp) * 1000;
    const now = Date.now();

    const targetTime = expiryTime - this.REFRESH_BUFFER;
    const delay = targetTime - now;

    console.log(`[AuthManager] Time check: 
      Now (Local):    ${new Date(now).toISOString()}
      Expiry (Token): ${new Date(expiryTime).toISOString()}
      Refresh Target: ${new Date(targetTime).toISOString()}
      Delay (sec):    ${Math.round(delay / 1000)}s`);

    if (delay <= 0) {
      console.log("[AuthManager] Already in/past window. Triggering now.");
      this.checkAndRefresh();
    } else {
      console.log(`[AuthManager] Timer set for ${Math.round(delay / 1000)}s.`);
      this.timeoutId = window.setTimeout(() => {
        console.log("[AuthManager] Scheduled Timer Fired!");
        this.checkAndRefresh();
      }, delay);
    }
  }

  private async checkAndRefresh() {
    console.log("[AuthManager] Executing REFRESH request...");
    try {
      await api.post("/auth/refresh");
      queryClient.invalidateQueries({ queryKey: authKeys.me() });
      console.log("[AuthManager] Refresh Success.");
    } catch (error) {
      console.error("[AuthManager] Refresh Failed:", error);
    } finally {
      this.scheduleNextCheck();
    }
  }
}

export const sessionManager = new AuthSessionManager();
