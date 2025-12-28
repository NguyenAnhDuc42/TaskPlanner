import SignInForm from "./components/sign-in-form";

export function SignInPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      <div className="w-full max-w-md">
        <div className="mb-8 text-center">
          <h1 className="text-3xl font-bold tracking-tight">Create an account</h1>
          <p className="mt-2 text-muted-foreground">Get started with your free account today</p>
        </div>

        <SignInForm />
      </div>
    </div>
  );
}