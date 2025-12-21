import { API_URL } from "@/env";

const REFRESH_API = API_URL + '/auth/refresh'

export async function refreshTokens(cookieHeader: string): Promise<boolean> {
  const res = await fetch(REFRESH_API, {
    method: 'POST',
    headers: { cookie: cookieHeader },
    credentials: 'include',
  })

  return res.ok
}