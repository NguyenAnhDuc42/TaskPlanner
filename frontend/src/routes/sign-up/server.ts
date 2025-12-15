import { createServerFn } from '@tanstack/react-start'
import { z } from 'zod'

const registerInput = z.object({
  userName: z.string().min(2),
  email: z.email(),
  password: z.string().min(8),
})

export const register = createServerFn({ method: 'POST' })
  .inputValidator(registerInput)
  .handler(async ({ data }) => {
    const res = await fetch('http://localhost:5000/api/auth/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
      credentials: 'include',
    })

    if (!res.ok) {
      // attempt to surface backend message
      const text = await res.text().catch(() => null)
      throw new Error(text || 'Registration failed')
    }
  })
