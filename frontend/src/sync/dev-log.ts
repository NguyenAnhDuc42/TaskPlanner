// Sync-system debug logging — silent in production builds, verbose in dev.
export function devLog(...args: unknown[]): void {
  if (import.meta.env.DEV) {
    console.log(...args)
  }
}

export function devError(...args: unknown[]): void {
  if (import.meta.env.DEV) {
    console.error(...args)
  }
}
