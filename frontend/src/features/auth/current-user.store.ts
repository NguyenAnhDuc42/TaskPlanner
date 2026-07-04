import { makeAutoObservable, runInAction } from "mobx";
import { api } from "@/lib/api-client";
import type { User } from "./types";

// Module-level singleton, not tied to RootStore — this needs to be readable from
// AuthProvider/useAuth, which sits in main.tsx OUTSIDE the router (and thus outside
// RootStoreProvider, which only exists once __root.tsx's AppShell mounts). Every useUser()
// caller (there are ~10 across the app) shares this one instance/fetch instead of each firing
// its own /auth/me request, mirroring the dedup RTK Query used to give for free.
class CurrentUserStore {
  data: User | null = null;
  isFetching = false;
  hasFetchedOnce = false;
  private inFlight: Promise<void> | null = null;

  constructor() {
    makeAutoObservable(this);
  }

  get isLoading(): boolean {
    return this.isFetching && !this.hasFetchedOnce;
  }

  ensureLoaded(): void {
    if (this.hasFetchedOnce || this.isFetching) return;
    void this.refetch();
  }

  async refetch(): Promise<void> {
    if (this.inFlight) return this.inFlight;

    runInAction(() => { this.isFetching = true; });

    const promise = api.get<User>("/auth/me")
      .then(({ data }) => runInAction(() => { this.data = data; }))
      .catch(() => runInAction(() => { this.data = null; }))
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
  }
}

export const currentUserStore = new CurrentUserStore();
