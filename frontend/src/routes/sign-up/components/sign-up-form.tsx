import { useNavigate } from '@tanstack/react-router'
import { useState } from 'react'
import z from 'zod'
import { register } from '../server'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import { Icons } from '@/components/icons'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Separator } from '@/components/ui/separator'
import { Checkbox } from '@/components/ui/checkbox'


const signUpSchema = z.object({
  name: z.string().min(2, 'Name must be at least 2 characters'),
  email: z.string().email('Invalid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
})
type SignUpValues = z.infer<typeof signUpSchema>
type SignUpErrors = Partial<Record<keyof SignUpValues, string>>

export function SignUpForm() {
  const navigate = useNavigate()
  const [isLoading, setIsLoading] = useState(false)
  const [oauthLoading, setOauthLoading] = useState<'google' | 'github' | null>(
    null,
  )

  const [values, setValues] = useState<SignUpValues>({
    name: '',
    email: '',
    password: '',
  })
  const [errors, setErrors] = useState<SignUpErrors>({})

  function validateField<K extends keyof SignUpValues>(
    field: K,
    value: string,
  ) {
    // use the per-field schema (zod internals) for fast single-field validation
    const shape = signUpSchema.shape[field] as z.ZodTypeAny
    const res = shape.safeParse(value)
    setErrors((prev) => ({
      ...prev,
      [field]: res.success ? undefined : res.error.issues[0].message,
    }))
  }

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsLoading(true)

    // full-form validation
    const parsed = signUpSchema.safeParse(values)
    if (!parsed.success) {
      const fieldErrors: SignUpErrors = {}
      parsed.error.issues.forEach((i) => {
        const key = i.path[0] as keyof SignUpValues
        fieldErrors[key] = i.message
      })
      setErrors(fieldErrors)
      setIsLoading(false)
      return
    }

    try {
      await register({
        userName: parsed.data.name,
        email: parsed.data.email,
        password: parsed.data.password,
      })

      // navigate to sign-in after success
      navigate({ to: '/sign-in' })
    } catch (err) {
      // reveal a single top-level error for now — adapt to your toast system
      console.error('register failed', err)
      setErrors((p) => ({
        ...p,
        password: (err as Error)?.message ?? 'Registration failed',
      }))
    } finally {
      setIsLoading(false)
    }
  }

  function handleOAuth(provider: 'google' | 'github') {
    setOauthLoading(provider)
    // Kick off OAuth on backend (keeps your original redirect behaviour)
    window.location.href = `/api/auth/external-login/${provider}`
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Create account</CardTitle>
        <CardDescription>
          Enter your information to create your account
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-4">
        <div className="grid gap-2">
          <Button
            variant="outline"
            type="button"
            disabled={!!oauthLoading}
            onClick={() => handleOAuth('google')}
          >
            {oauthLoading === 'google' ? (
              <Icons.spinner className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Icons.google className="mr-2 h-4 w-4" />
            )}
            Continue with Google
          </Button>

          <Button
            variant="outline"
            type="button"
            disabled={!!oauthLoading}
            onClick={() => handleOAuth('github')}
          >
            {oauthLoading === 'github' ? (
              <Icons.spinner className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Icons.gitHub className="mr-2 h-4 w-4" />
            )}
            Continue with GitHub
          </Button>
        </div>

        <div className="relative">
          <div className="absolute inset-0 flex items-center">
            <Separator />
          </div>
          <div className="relative flex justify-center text-xs uppercase">
            <span className="bg-card px-2 text-muted-foreground">
              Or continue with
            </span>
          </div>
        </div>

        <form onSubmit={onSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="name">Full name</Label>
            <Input
              id="name"
              name="name"
              type="text"
              placeholder="John Doe"
              autoComplete="name"
              required
              disabled={isLoading}
              value={values.name}
              onChange={(e) => {
                const v = e.target.value
                setValues((p) => ({ ...p, name: v }))
                validateField('name', v)
              }}
            />
            {errors.name && (
              <p className="text-sm text-destructive">{errors.name}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              name="email"
              type="email"
              placeholder="name@example.com"
              autoComplete="email"
              required
              disabled={isLoading}
              value={values.email}
              onChange={(e) => {
                const v = e.target.value
                setValues((p) => ({ ...p, email: v }))
                validateField('email', v)
              }}
            />
            {errors.email && (
              <p className="text-sm text-destructive">{errors.email}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="password">Password</Label>
            <Input
              id="password"
              name="password"
              type="password"
              autoComplete="new-password"
              required
              disabled={isLoading}
              value={values.password}
              onChange={(e) => {
                const v = e.target.value
                setValues((p) => ({ ...p, password: v }))
                validateField('password', v)
              }}
            />
            <p className="text-xs text-muted-foreground">
              Must be at least 8 characters long
            </p>
            {errors.password && (
              <p className="text-sm text-destructive">{errors.password}</p>
            )}
          </div>

          <div className="flex items-center space-x-2">
            <Checkbox id="terms" required />
            <label
              htmlFor="terms"
              className="text-sm leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
            >
              I agree to the{' '}
              <a
                href="/terms"
                className="font-medium underline underline-offset-4"
              >
                terms of service
              </a>{' '}
              and{' '}
              <a
                href="/privacy"
                className="font-medium underline underline-offset-4"
              >
                privacy policy
              </a>
            </label>
          </div>

          <Button type="submit" className="w-full" disabled={isLoading}>
            {isLoading && (
              <Icons.spinner className="mr-2 h-4 w-4 animate-spin" />
            )}
            Create account
          </Button>
        </form>
      </CardContent>

      <CardFooter className="flex justify-center">
        <p className="text-sm text-muted-foreground">
          Already have an account?{' '}
          <a
            href="/sign-in"
            className="font-medium text-foreground hover:underline"
          >
            Sign in
          </a>
        </p>
      </CardFooter>
    </Card>
  )
}
