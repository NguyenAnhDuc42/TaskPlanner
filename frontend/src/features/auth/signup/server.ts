import { createServerFn } from '@tanstack/react-start'
import { z } from 'zod'

const API = process.env.API_URL

export const signUpSchema = z.object({
  name: z
    .string()
    .min(2, 'Name must be at least 2 characters')
    .max(50, 'Name must be less than 50 characters'),
  email: z.email('Invalid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
})

export const register = createServerFn({ method: 'POST' })
  .inputValidator(
    signUpSchema.transform((v) => ({
      userName: v.name,
      email: v.email,
      password: v.password,
    })),
  )
  .handler(async ({ data }) => {
    // console.log('DEBUG: apiUrl =', apiUrl)
    // console.log('DEBUG: data =', JSON.stringify(data))

    const res = await fetch(`${API}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
      credentials: 'include',
    })
    // const responseText = await res.text()
    // console.log('DEBUG: Response status =', res.status)
    // console.log('DEBUG: Response body =', responseText)
    if (!res.ok) {
      const problem = await res.json(); // The RFC 7807 payload
      
      // CRITICAL: Throw a Response, not a new Error()
      // This preserves the 400/409 status in the Network tab
      throw new Response(JSON.stringify(problem), {
        status: res.status,
        headers: { 'Content-Type': 'application/problem+json' },
      });
    }

    return res.json();
  })
