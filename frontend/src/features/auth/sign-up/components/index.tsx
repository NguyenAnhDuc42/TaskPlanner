import { SignUpForm } from "./sign-up-form";

export function SignUpPage() {
  return (
    <div className="min-h-screen flex bg-background">
      {/* Left decorative panel */}
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
            Start shipping<br />in minutes.
          </h2>
          <p className="text-sm text-muted-foreground/60 leading-relaxed">
            Free to start. No credit card required. Join thousands of teams already building with TaskPlanner.
          </p>

          <div className="mt-8 space-y-3">
            {[
              "Unlimited personal projects",
              "Custom workflows & statuses",
              "Drag & drop task management",
            ].map((f) => (
              <div key={f} className="flex items-center gap-2.5 text-xs text-muted-foreground/60">
                <div className="h-1.5 w-1.5 rounded-full bg-primary/60 shrink-0" />
                {f}
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Right form panel */}
      <div className="flex-1 flex items-center justify-center p-6">
        <div className="w-full max-w-sm space-y-8">
          {/* Mobile logo */}
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
            <h1 className="text-2xl font-black text-foreground/95 tracking-tight">Create your account</h1>
            <p className="mt-1 text-sm text-muted-foreground/55">
              Get started for free — no credit card required
            </p>
          </div>

          <SignUpForm />
        </div>
      </div>
    </div>
  );
}
