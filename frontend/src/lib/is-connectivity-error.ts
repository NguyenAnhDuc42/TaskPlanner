import axios from "axios";

// A raw network failure (no response at all — e.g. real offline, DNS failure, ECONNREFUSED
// straight to the backend) is not the only way "the server is unreachable" shows up. Behind the
// Vite dev proxy (and any real reverse proxy in production), a downed backend gets synthesized
// into an actual HTTP response — 502/503/504 — which axios treats as `err.response` being
// present. Mutations use this to decide "queue and retry later" vs "the server rejected this,
// roll back" — treating only `!err.response` as connectivity loss misses the proxied case
// entirely, which then wrongly rolls back an optimistic write just because the backend process
// happened to be down while the network itself was fine.
export function isConnectivityError(err: unknown): boolean {
  if (!axios.isAxiosError(err)) return false;
  if (!err.response) return true;
  return [502, 503, 504].includes(err.response.status);
}

// A delete that 404s means the entity is already gone — the delete's own goal is already
// satisfied. Mutations' delete() methods must NOT roll back (re-upsert the entity) on this
// specific case the way they do for other failures: rolling back would resurrect an entity that's
// correctly, permanently gone server-side, just because this client's delete request happened to
// arrive after someone else's (or its own earlier retry) already succeeded.
export function isNotFoundError(err: unknown): boolean {
  return axios.isAxiosError(err) && err.response?.status === 404;
}
