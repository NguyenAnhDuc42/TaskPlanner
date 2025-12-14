"use client"

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Lock } from "lucide-react"
import { AnimatedTabs, AnimatedTabsContent } from "@/components/auth/animated-tabs"
import { LoginForm } from "@/components/auth/login-form"
import { RegisterForm } from "@/components/auth/register-form"
import { SocialLogin } from "@/components/auth/social-login"

export default function AuthPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-zinc-950 p-3">
      <Card className="w-full max-w-xs bg-zinc-900/95 border border-zinc-800 shadow-xl">
        <CardHeader className="space-y-2 pb-4 pt-5">
          <div className="flex justify-center">
            <div className="w-10 h-10 bg-zinc-100 rounded-lg flex items-center justify-center">
              <Lock className="w-5 h-5 text-zinc-900" />
            </div>
          </div>
          <div className="text-center space-y-1">
            <CardTitle className="text-xl font-semibold text-zinc-50">Welcome</CardTitle>
            <CardDescription className="text-xs text-zinc-400">Sign in or create account</CardDescription>
          </div>
        </CardHeader>

        <CardContent className="px-5 pb-5">
          <AnimatedTabs defaultValue="login">
            <AnimatedTabsContent value="login">
              <LoginForm />
            </AnimatedTabsContent>

            <AnimatedTabsContent value="register">
              <RegisterForm />
            </AnimatedTabsContent>
          </AnimatedTabs>

          <SocialLogin />
        </CardContent>
      </Card>
    </div>
  )
}
