import { ResetPasswordForm } from "./reset-password-form";

interface Props {
  token: string;
}

export function ResetPasswordPage({ token }: Props) {
  return (
    <div className="min-h-screen flex bg-background">
      <div className="hidden lg:flex lg:w-1/2 relative overflow-hidden bg-card/20 border-r border-border/20 items-center justify-center p-12">
        <div className="absolute top-1/3 left-1/4 w-72 h-72 rounded-full bg-primary/10 blur-[100px] pointer-events-none" />
        <div className="absolute bottom-1/4 right-1/3 w-56 h-56 rounded-full bg-violet-500/8 blur-[80px] pointer-events-none" />

        <div className="relative z-10 max-w-sm">
          <div className="flex items-center gap-2 mb-10">
            <div className="h-7 w-7 rounded-md bg-primary/90 flex items-center justify-center shadow-lg shadow-primary/30">
              <svg viewBox="0 0 16 16" fill="white" className="h-4 w-4">
                <rect x="2" y="2" width="5" height="5" rx="1" />
                <rect x="9" y="2" width="5" height="5" rx="1" opacity="0.7" />
                <rect x="2" y="9" width="5" height="5" rx="1" opacity="0.7" />
                <rect x="9" y="9" width="5" height="5" rx="1" opacity="0.4" />
              </svg>
            </div>
            <span className="text-sm font-black text-foreground/90 tracking-tight">TaskPlanner</span>
          </div>
          <h2 className="text-2xl font-black text-foreground/90 leading-tight mb-3">
            Choose a strong<br />new password.
          </h2>
          <p className="text-sm text-muted-foreground/60 leading-relaxed">
            Pick something you haven't used before. A mix of uppercase, lowercase, and numbers works best.
          </p>
        </div>
      </div>

      <div className="flex-1 flex items-center justify-center p-6">
        <div className="w-full max-w-sm space-y-8">
          <div className="lg:hidden flex items-center gap-2 justify-center">
            <div className="h-7 w-7 rounded-md bg-primary/90 flex items-center justify-center shadow-lg shadow-primary/30">
              <svg viewBox="0 0 16 16" fill="white" className="h-4 w-4">
                <rect x="2" y="2" width="5" height="5" rx="1" />
                <rect x="9" y="2" width="5" height="5" rx="1" opacity="0.7" />
                <rect x="2" y="9" width="5" height="5" rx="1" opacity="0.7" />
                <rect x="9" y="9" width="5" height="5" rx="1" opacity="0.4" />
              </svg>
            </div>
            <span className="text-sm font-black text-foreground/90">TaskPlanner</span>
          </div>

          <div>
            <h1 className="text-2xl font-black text-foreground/95 tracking-tight">Set a new password</h1>
            <p className="mt-1 text-sm text-muted-foreground/55">
              Your new password must be at least 8 characters
            </p>
          </div>

          <ResetPasswordForm token={token} />
        </div>
      </div>
    </div>
  );
}
