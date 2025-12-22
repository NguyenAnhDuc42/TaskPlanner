import { createFileRoute } from '@tanstack/react-router'
import { SignUpPage } from '@/features/auth/signup/page'

export const Route = createFileRoute('/auth/sign-up')({
  component: SignUpPage,
  ssr: false,
})
