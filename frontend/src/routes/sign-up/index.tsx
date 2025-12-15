import { createFileRoute } from '@tanstack/react-router'
import { SignUpForm } from './components/SignUpForm';

export const Route = createFileRoute('/sign-up/')({
  component: SignUpPage,
})

export function SignUpPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      <div className="w-full max-w-md">
        <div className="mb-8 text-center">
          <h1 className="text-3xl font-bold tracking-tight">Create an account</h1>
          <p className="mt-2 text-muted-foreground">Get started with your free account today</p>
        </div>

        <SignUpForm />
      </div>
    </div>
  );
}
