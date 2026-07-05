import { makeAutoObservable, runInAction } from "mobx";
import { api } from "@/lib/api-client";
import { isConnectivityError } from "@/lib/is-connectivity-error";
import type { User } from "./types";

const CACHE_KEY = "cached-user";

function readCache(): User | null {
  try {
    const raw = localStorage.getItem(CACHE_KEY);
    return raw ? (JSON.parse(raw) as User) : null;
  } catch {
    return null;
  }
}

function writeCache(user: User | null): void {
  try {
    if (user) localStorage.setItem(CACHE_KEY, JSON.stringify(user));
    else localStorage.removeItem(CACHE_KEY);
  } catch {
    // localStorage unavailable (private browsing, quota) — not fatal, just no offline identity cache
  }
}


class CurrentUserStore {
  data: User | null = readCache();
  isFetching = false;
  hasFetchedOnce = false;
  private inFlight: Promise<void> | null = null;

  constructor() {
    makeAutoObservable(this);
  }

  get isLoading(): boolean {
    return this.isFetching && !this.hasFetchedOnce && !this.data;
  }

  ensureLoaded(): void {
    if (this.hasFetchedOnce || this.isFetching) return;
    void this.refetch();
  }

  async refetch(): Promise<void> {
    if (this.inFlight) return this.inFlight;

    runInAction(() => { this.isFetching = true; });

    const promise = api.get<User>("/auth/me")
      .then(({ data }) => runInAction(() => {
        this.data = data;
        writeCache(data);
      }))
      .catch((err) => {
        if (isConnectivityError(err)) return; // keep whatever identity we already had
        runInAction(() => {
          this.data = null;
          writeCache(null);
        });
      })
      .finally(() => runInAction(() => {
        this.isFetching = false;
        this.hasFetchedOnce = true;
        this.inFlight = null;
      }));

    this.inFlight = promise;
    return promise;
  }

  clear(): void {
    this.data = null;
    this.hasFetchedOnce = false;
    writeCache(null);
  }
}

export const currentUserStore = new CurrentUserStore();
