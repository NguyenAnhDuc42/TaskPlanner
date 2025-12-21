import { createServerFn } from '@tanstack/react-start'
import { z } from 'zod'
import { API_URL } from '@/env'


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

    let res: Response
    try {
      res = await fetch(`${API_URL}/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
        credentials: 'include',
      })
      // const responseText = await res.text()
      // console.log('DEBUG: Response status =', res.status)
      // console.log('DEBUG: Response body =', responseText)
    } catch (error: any) {
      console.error('Registration fetch error:', error)
      throw new Error(`Connection failed: ${error?.message || 'Unknown error'}`)
    }

    if (!res.ok) {
      const message =
        (await res.json().catch(() => null))?.message ?? 'Registration failed'

      throw new Error(message)
    }
  })
