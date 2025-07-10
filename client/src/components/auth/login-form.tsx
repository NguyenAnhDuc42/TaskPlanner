"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Loader2, AlertCircle, Eye, EyeOff, Mail, Lock } from "lucide-react"
import { LoginSchema } from "@/features/auth/type"
import type { ErrorResponse } from "@/types/responses/error-response"
import { useLogin } from "@/features/auth/hooks"

export function LoginForm({ className = "" }: { className?: string }) {
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [showPassword, setShowPassword] = useState(false)
  const [clientErrors, setClientErrors] = useState<Record<string, string>>({})

  const loginMutation = useLogin()

  // Backend field errors
  function getBackendFieldErrors(error: unknown): Record<string, string[]> {
    if (
      error &&
      typeof error === 'object' &&
      'status' in error &&
      typeof (error as { status: unknown }).status === 'number' &&
      'extensions' in error &&
      typeof (error as { extensions: unknown }).extensions === 'object'
    ) {
      return ((error as ErrorResponse).extensions) || {};
    }
    return {};
  }
  const backendFieldErrors = getBackendFieldErrors(loginMutation.error);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target
    if (name === "email") setEmail(value)
    if (name === "password") setPassword(value)
    setClientErrors((prev) => {
      const newErrors = { ...prev }
      delete newErrors[name]
      return newErrors
    })
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    // Zod validation
    const result = LoginSchema.safeParse({ email, password })
    if (!result.success) {
      const errors: Record<string, string> = {}
      result.error.errors.forEach(err => {
        if (err.path[0]) errors[err.path[0]] = err.message
      })
      setClientErrors(errors)
      return
    }
    setClientErrors({})
    loginMutation.mutate({ email, password })
  }

  // General error (not field-specific)
  function getErrorResponse(error: unknown): ErrorResponse | null {
    if (
      error &&
      typeof error === 'object' &&
      'status' in error &&
      typeof (error as { status: unknown }).status === 'number'
    ) {
      return error as ErrorResponse;
    }
    return null;
  }
  const error = getErrorResponse(loginMutation.error);
  const isFormError = error && error.status !== 400;

  const getFieldErrorMessage = (field: string) => {
    return clientErrors[field] || backendFieldErrors[field]?.[0]
  }

  return (
    <form onSubmit={handleSubmit} className={`space-y-3 ${className}`}>
      {isFormError && error && (
        <Alert variant="destructive" className="border-red-800 bg-red-950/50 py-2">
          <AlertCircle className="h-3 w-3" />
          <AlertDescription className="text-xs text-red-300">
            {error.detail || error.title}
          </AlertDescription>
        </Alert>
      )}

      <div className="space-y-1.5">
        <Label htmlFor="login-email" className="text-xs font-medium text-zinc-200">
          Email
        </Label>
        <div className="relative">
          <Mail className="absolute left-2.5 top-1/2 transform -translate-y-1/2 text-zinc-500 w-3.5 h-3.5" />
          <Input
            id="login-email"
            name="email"
            type="text"
            placeholder="Enter email"
            value={email}
            onChange={handleChange}
            disabled={loginMutation.isPending}
            className={`pl-8 h-8 text-sm bg-zinc-800 border-zinc-700 text-zinc-100 placeholder:text-zinc-500 focus:border-zinc-400 focus:ring-zinc-400/20 transition-all duration-150 ${
              getFieldErrorMessage("email") ? "border-red-600 focus:border-red-600" : ""
            }`}
          />
        </div>
        {getFieldErrorMessage("email") && (
          <p className="text-xs text-red-400 flex items-center gap-1">
            <AlertCircle className="w-3 h-3" />
            {getFieldErrorMessage("email")}
          </p>
        )}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="login-password" className="text-xs font-medium text-zinc-200">
          Password
        </Label>
        <div className="relative">
          <Lock className="absolute left-2.5 top-1/2 transform -translate-y-1/2 text-zinc-500 w-3.5 h-3.5" />
          <Input
            id="login-password"
            name="password"
            type={showPassword ? "text" : "password"}
            placeholder="Enter password"
            value={password}
            onChange={handleChange}
            disabled={loginMutation.isPending}
            className={`pl-8 pr-8 h-8 text-sm bg-zinc-800 border-zinc-700 text-zinc-100 placeholder:text-zinc-500 focus:border-zinc-400 focus:ring-zinc-400/20 transition-all duration-150 ${
              getFieldErrorMessage("password") ? "border-red-600 focus:border-red-600" : ""
            }`}
          />
          <button
            type="button"
            onClick={() => setShowPassword(!showPassword)}
            className="absolute right-2.5 top-1/2 transform -translate-y-1/2 text-zinc-500 hover:text-zinc-300 transition-colors"
          >
            {showPassword ? <EyeOff className="w-3.5 h-3.5" /> : <Eye className="w-3.5 h-3.5" />}
          </button>
        </div>
        {getFieldErrorMessage("password") && (
          <p className="text-xs text-red-400 flex items-center gap-1">
            <AlertCircle className="w-3 h-3" />
            {getFieldErrorMessage("password")}
          </p>
        )}
      </div>

      <div className="flex items-center justify-between text-xs pt-1">
        <label className="flex items-center space-x-1.5 cursor-pointer">
          <input
            type="checkbox"
            className="w-3 h-3 text-zinc-600 bg-zinc-800 border-zinc-600 rounded focus:ring-zinc-500 focus:ring-1"
          />
          <span className="text-zinc-400">Remember</span>
        </label>
        <a href="/forgot-password" className="text-zinc-300 hover:text-zinc-100 transition-colors">
          Forgot password?
        </a>
      </div>

      <Button
        type="submit"
        className="w-full h-8 text-sm bg-zinc-100 text-zinc-900 hover:bg-zinc-200 font-medium transition-all duration-200"
        disabled={loginMutation.isPending}
      >
        {loginMutation.isPending ? (
          <>
            <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
            Signing in...
          </>
        ) : (
          "Sign in"
        )}
      </Button>
    </form>
  )
}
