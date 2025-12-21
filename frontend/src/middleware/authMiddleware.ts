import { createMiddleware } from '@tanstack/react-start'
import { API_URL } from '@/env'




export const authMiddleware = createMiddleware().server(async ({ next, request }) => {

    const url = new URL(request.url)
    const path = url.pathname

    if (path.startsWith('/auth') || path.startsWith('/_static') || path.includes('.')) {
    return next()
    }
    const cookieHeader = request.headers.get('cookie') ?? ''
    const accessToken = getCookie(cookieHeader, 'act')
    const refreshToken = getCookie(cookieHeader, 'rft')
    
    if (!accessToken && !refreshToken) {
      return Response.redirect(
        `/auth/sign-in?redirect=${encodeURIComponent(path)}`, 
        302
      )
    }
    if (!accessToken && refreshToken) {
    const refreshed = await refreshTokens(cookieHeader)
      if (!refreshed) {
        return Response.redirect( `/auth/sign-in?redirect=${encodeURIComponent(path)}`, 302)
      }
    }
    return next()
  },
)

async function refreshTokens(cookieHeader: string): Promise<boolean> {
  const res = await fetch(`${API_URL}/auth/refresh`, {
    method: 'POST',
    headers: { cookie: cookieHeader },
    credentials: 'include',
  })

  return res.ok
}


function getCookie(cookieHeader: string, name: string): string | null {
  if (!cookieHeader) return null
  for (const part of cookieHeader.split(';')) {
    const [k, ...v] = part.split('=')
    if (k.trim() === name) return v.join('=').trim()
  }
  return null
}