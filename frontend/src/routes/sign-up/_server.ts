import { createServerFn } from '@tanstack/react-start'
import { z } from 'zod'
import { API_BASE } from '@/env'

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
    const res = await fetch(`${API_BASE}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
      credentials: 'include',
    })

    if (!res.ok) {
      const message =
        (await res.json().catch(() => null))?.message ??
        'Registration failed'

      throw new Error(message)
    }

  })
