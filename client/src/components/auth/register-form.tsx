"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Loader2, AlertCircle, Eye, EyeOff, Mail, Lock, User } from "lucide-react"
import { RegisterSchema } from "@/features/auth/type"

import type { ErrorResponse } from "@/types/responses/error-response"
import { useRegister } from "@/features/auth/hooks"

export function RegisterForm({ className = "" }: { className?: string }) {
  const [username, setUsername] = useState("")
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [showPassword, setShowPassword] = useState(false)
  const [clientErrors, setClientErrors] = useState<Record<string, string>>({})

  const registerMutation = useRegister();

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
  const backendFieldErrors = getBackendFieldErrors(registerMutation.error);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target
    if (name === "username") setUsername(value)
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
    const result = RegisterSchema.safeParse({ username, email, password })
    if (!result.success) {
      const errors: Record<string, string> = {}
      result.error.errors.forEach(err => {
        if (err.path[0]) errors[err.path[0]] = err.message
      })
      setClientErrors(errors)
      return
    }
    setClientErrors({})
    registerMutation.mutate({ username, email, password })
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
  const error = getErrorResponse(registerMutation.error);
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
        <Label htmlFor="register-username" className="text-xs font-medium text-zinc-200">
          Username
        </Label>
        <div className="relative">
          <User className="absolute left-2.5 top-1/2 transform -translate-y-1/2 text-zinc-500 w-3.5 h-3.5" />
          <Input
            id="register-username"
            name="username"
            type="text"
            placeholder="Enter username"
            value={username}
            onChange={handleChange}
            disabled={registerMutation.isPending}
            className={`pl-8 h-8 text-sm bg-zinc-800 border-zinc-700 text-zinc-100 placeholder:text-zinc-500 focus:border-zinc-400 focus:ring-zinc-400/20 transition-all duration-150 ${
              getFieldErrorMessage("username") ? "border-red-600 focus:border-red-600" : ""
            }`}
          />
        </div>
        {getFieldErrorMessage("username") && (
          <p className="text-xs text-red-400 flex items-center gap-1">
            <AlertCircle className="w-3 h-3" />
            {getFieldErrorMessage("username")}
          </p>
        )}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="register-email" className="text-xs font-medium text-zinc-200">
          Email
        </Label>
        <div className="relative">
          <Mail className="absolute left-2.5 top-1/2 transform -translate-y-1/2 text-zinc-500 w-3.5 h-3.5" />
          <Input
            id="register-email"
            name="email"
            type="text"
            placeholder="Enter email"
            value={email}
            onChange={handleChange}
            disabled={registerMutation.isPending}
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
        <Label htmlFor="register-password" className="text-xs font-medium text-zinc-200">
          Password
        </Label>
        <div className="relative">
          <Lock className="absolute left-2.5 top-1/2 transform -translate-y-1/2 text-zinc-500 w-3.5 h-3.5" />
          <Input
            id="register-password"
            name="password"
            type={showPassword ? "text" : "password"}
            placeholder="Enter password"
            value={password}
            onChange={handleChange}
            disabled={registerMutation.isPending}
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

      <div className="text-xs text-zinc-500 pt-1">
        By creating an account, you agree to our{" "}
        <a href="/terms" className="text-zinc-300 hover:text-zinc-100 transition-colors underline">
          Terms
        </a>{" "}
        and{" "}
        <a href="/privacy" className="text-zinc-300 hover:text-zinc-100 transition-colors underline">
          Privacy Policy
        </a>
        .
      </div>

      <Button
        type="submit"
        className="w-full h-8 text-sm bg-zinc-100 text-zinc-900 hover:bg-zinc-200 font-medium transition-all duration-200"
        disabled={registerMutation.isPending}
      >
        {registerMutation.isPending ? (
          <>
            <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
            Creating...
          </>
        ) : (
          "Create account"
        )}
      </Button>
    </form>
  )
}
