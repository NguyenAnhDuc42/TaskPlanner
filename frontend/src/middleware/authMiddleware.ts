import { createMiddleware, createServerFn } from '@tanstack/react-start'




export const authMiddleware = createMiddleware().server(async ({ next, request }) => {

    const url = new URL(request.url)
    const path = url.pathname

    if (path.startsWith('/auth') || path.startsWith('/_static') || path.includes('.')) {
    return next()
    }
    const cookieHeader = request.headers.get('cookie') ?? ''
    const accessToken = getCookie(cookieHeader, 'act')
    const refreshToken = getCookie(cookieHeader, 'rft')
    
    if (path.startsWith('/auth')) {
        if (accessToken || refreshToken) {
        return Response.redirect('/', 302)
        }
        return next() 
    }
   


    if (!accessToken && !refreshToken) {
    return Response.redirect(`/auth/sign-in?redirect=${encodeURIComponent(path)}`, 302)
    }
    

    return next()
  },
)

const refrehToken = createServerFn({ method: 'POST' }).handler(async () => {

})

function getCookie(cookieHeader: string, name: string): string | null {
  if (!cookieHeader) return null
  for (const part of cookieHeader.split(';')) {
    const [k, ...v] = part.split('=')
    if (k.trim() === name) return v.join('=').trim()
  }
  return null
}