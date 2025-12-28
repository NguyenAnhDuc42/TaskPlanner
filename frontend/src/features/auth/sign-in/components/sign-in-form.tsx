import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Checkbox } from "@/components/ui/checkbox"
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import { Separator } from "@/components/ui/separator"
import {  useForm } from "@tanstack/react-form"
import { useNavigate } from "@tanstack/react-router"

export default function SignInForm() {
  const navigate = useNavigate()
  const form = useForm({
    defaultValues: {
      email: '',
      password: '',
    },
  })

  return (
    <Card>
      <CardHeader>
        <CardTitle>Sign in</CardTitle>
        <CardDescription>
          Enter your credentials to access your account
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-4">
        <div className="grid gap-2">
          <Button
            variant="outline"
            type="button"
            onClick={() =>
              (window.location.href = '/api/auth/external-login/google')
            }
          >
            Continue with Google
          </Button>
          <Button
            variant="outline"
            type="button"
            onClick={() =>
              (window.location.href = '/api/auth/external-login/github')
            }
          >
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

        <form
          id="sign-in-form"
          onSubmit={(e) => {
            e.preventDefault()
            form.handleSubmit()
          }}
          className="space-y-4"
          noValidate
        >
          <FieldGroup>
            <form.Field
              name="email"
              children={(field) => {
                const isInvalid =
                  field.state.meta.isTouched && !field.state.meta.isValid
                return (
                  <Field data-invalid={isInvalid}>
                    <FieldLabel htmlFor={field.name}>Email</FieldLabel>
                    <Input
                      id={field.name}
                      name={field.name}
                      type="email"
                      value={field.state.value}
                      onBlur={field.handleBlur}
                      onChange={(e) => field.handleChange(e.target.value)}
                      placeholder="name@example.com"
                      autoComplete="email"
                      required
                      aria-invalid={isInvalid}
                    />
                    {isInvalid ? (
                      <FieldError errors={field.state.meta.errors} />
                    ) : (
                      <FieldDescription />
                    )}
                  </Field>
                )
              }}
            />

            <form.Field
              name="password"
              children={(field) => {
                const isInvalid =
                  field.state.meta.isTouched && !field.state.meta.isValid
                return (
                  <Field data-invalid={isInvalid}>
                    <FieldLabel htmlFor={field.name}>Password</FieldLabel>
                    <Input
                      id={field.name}
                      name={field.name}
                      type="password"
                      value={field.state.value}
                      onBlur={field.handleBlur}
                      onChange={(e) => field.handleChange(e.target.value)}
                      autoComplete="current-password"
                      required
                      aria-invalid={isInvalid}
                    />
                    {isInvalid ? (
                      <FieldError errors={field.state.meta.errors} />
                    ) : (
                      <FieldDescription />
                    )}
                  </Field>
                )
              }}
            />
          </FieldGroup>
        </form>
      </CardContent>

      <CardFooter className="flex justify-center">
        <div className="w-full">
          <div className="flex items-center justify-between mb-3">
            <label className="flex items-center space-x-2 text-sm">
              <Checkbox id="remember" />
              <span>Remember me</span>
            </label>
            <a className="text-sm underline" href="/forgot-password">
              Forgot password?
            </a>
          </div>

          <Button type="submit" form="sign-in-form" className="w-full">
            Sign in
          </Button>
        </div>
      </CardFooter>
      <CardFooter className="flex justify-center">
        <p className="text-sm text-muted-foreground">
          Dont have an account?{' '}
          <button
            type="button" // TODO: Fix this type error
            onClick={() => navigate({ to: '/auth/sign-up' })}
            className="font-medium text-foreground hover:underline"
          >
            Sign up
          </button>
        </p>
      </CardFooter>
    </Card>
  )
}
