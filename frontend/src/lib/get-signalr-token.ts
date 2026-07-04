import { api } from '@/lib/api-client'


export async function getSignalRToken(): Promise<string | null> {
  try {
    const { data } = await api.get<{ accessToken: string }>('/auth/signalr-token')
    return data.accessToken
  } catch (err) {
    console.error('[SignalR] Failed to fetch connection token:', err)
    return null
  }
}
