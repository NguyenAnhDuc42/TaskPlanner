
const API = process.env.API_URL

let refreshTimer: NodeJS.Timeout | null = null

export function setupRefreshTimer() {
  if (refreshTimer) {
    clearTimeout(refreshTimer)
    refreshTimer = null
  }

  const expiry = getTokenExpiry('act')

  if (!expiry) {
    // No token, no timer
    return
  }

  const now = Date.now()
  const timeUntilExpiry = expiry - now
  const refreshAt = timeUntilExpiry - 60 * 1000 // 1 min before expiry

  if (refreshAt <= 0) {
    // Already expired or about to, refresh immediately
    refreshNow()
    return
  }

  // Set timer to refresh 1 min before expiry
  refreshTimer = setTimeout(() => {
    refreshNow()
  }, refreshAt)
}

async function refreshNow() {
  const res = await fetch(`${API}/auth/refresh`, {
    method: 'POST',
    credentials: 'include',
  })

  if (res.ok) {
    // Refresh succeeded, setup new timer with new token
    setupRefreshTimer()
  } else {
    // Refresh failed, redirect to login
    window.location.href = '/auth/sign-in'
  }
}

function getTokenExpiry(tokenName: string): number | null {
  if (typeof document === 'undefined') return null

  const cookies = document.cookie.split(';')
  for (const cookie of cookies) {
    const [name, value] = cookie.trim().split('=')
    if (name === tokenName) {
      try {
        const [, payload] = value.split('.')
        const decoded = JSON.parse(atob(payload))
        return decoded.exp * 1000 // milliseconds
      } catch {
        return null
      }
    }
  }
  return null
}
