import { createFileRoute } from '@tanstack/react-router'
import { SignInPage } from '@/features/auth/signin/page'

export const Route = createFileRoute('/auth/sign-in')({
  component: SignInPage,
  ssr: false,
})
