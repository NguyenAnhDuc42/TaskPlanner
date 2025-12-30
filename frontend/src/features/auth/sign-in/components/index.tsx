import SignInForm from "./sign-in-form";

export function SignInPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      <div className="w-full max-w-md">
        <div className="mb-8 text-center">
          <h1 className="text-3xl font-bold tracking-tight">Sign In</h1>
          <p className="mt-2 text-muted-foreground">
            Welcome back! Please enter your details.
          </p>
        </div>

        <SignInForm />
      </div>
    </div>
  );
}
