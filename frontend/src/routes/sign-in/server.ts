import { createServerFn } from '@tanstack/react-start'
import z from 'zod'

export const signInSchema = z.object({
  email: z.email('Invalid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
})

export const register = createServerFn({ method: 'POST' })
  .inputValidator(
    signInSchema.transform((v) => ({
      email: v.email,
      password: v.password,
    })),
  )
  .handler(async ({ data }) => {
    const res = await fetch('http://localhost:5000/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
      credentials: 'include',
    })

    if (!res.ok) {
      throw new Response(await res.text(), { status: res.status })
    }
  })
