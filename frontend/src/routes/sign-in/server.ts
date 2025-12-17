import { createServerFn } from '@tanstack/react-start'
import z from 'zod'
import { API_BASE } from '@/env';

export const signInSchema = z.object({
  email: z.string().email("Invalid email address"),
  password: z.string().min(1, "Password is required"),
});

export const login = createServerFn({ method: 'POST' })
  .inputValidator(
    signInSchema.transform((v) => ({
      email: v.email,
      password: v.password,
    })),
  )
  .handler(async ({ data }) => {
    const res = await fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
      credentials: 'include',
    })

    if (!res.ok) {
      // prefer structured JSON { field?, message? } if backend returns it
      try {
        const json = await res.json();
        const message = json?.message ?? JSON.stringify(json);
        throw new Response(String(message), { status: res.status, headers: { "Content-Type": "application/json" } });
      } catch {
        const text = (await res.text().catch(() => null)) ?? "Login failed";
        throw new Response(text, { status: res.status });
      }
    }
  })
