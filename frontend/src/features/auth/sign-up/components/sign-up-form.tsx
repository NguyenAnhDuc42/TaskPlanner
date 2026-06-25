import { useState } from "react";
import { toast } from "sonner";
import { useForm } from "@tanstack/react-form";
import { useRegister } from "../../auth-api";
import { useNavigate, Link } from "@tanstack/react-router";
import { Eye, EyeOff, Loader2, Github, Chrome, ArrowRight, Check } from "lucide-react";
import { cn } from "@/lib/utils";

const PASSWORD_RULES = [
  { label: "At least 8 characters", test: (v: string) => v.length >= 8 },
  { label: "One uppercase letter", test: (v: string) => /[A-Z]/.test(v) },
  { label: "One number", test: (v: string) => /[0-9]/.test(v) },
];

export function SignUpForm() {
  const { mutate: register, isPending } = useRegister();
  const navigate = useNavigate();
  const [showPassword, setShowPassword] = useState(false);
  const [passwordValue, setPasswordValue] = useState("");

  const form = useForm({
    defaultValues: { name: "", email: "", password: "" },
    onSubmit: async ({ value }) => {
      try {
        await register(value);
        toast.success("Account created! Welcome 🎉");
        localStorage.removeItem("lastWorkspaceId");
        navigate({ to: "/" });
      } catch (err: unknown) {
        const e = err as { data?: { detail?: string }; message?: string };
        toast.error(e.data?.detail || e.message || "Registration failed");
      }
    },
  });

  return (
    <div className="w-full space-y-6">
      {/* OAuth */}
      <div className="flex flex-col gap-3">
        <button
          type="button"
          disabled={isPending}
          onClick={() => (window.location.href = "/api/auth/external-login/google")}
          className="flex items-center justify-center gap-3 h-11 px-4 rounded-xl border border-border/80 bg-secondary hover:bg-muted shadow-sm hover:shadow-md text-sm font-bold text-foreground transition-all active:scale-[0.98] disabled:opacity-40 disabled:pointer-events-none"
        >
          <Chrome className="h-5 w-5" />
          Continue with Google
        </button>
        <button
          type="button"
          disabled={isPending}
          onClick={() => (window.location.href = "/api/auth/external-login/github")}
          className="flex items-center justify-center gap-3 h-11 px-4 rounded-xl border border-border/80 bg-secondary hover:bg-muted shadow-sm hover:shadow-md text-sm font-bold text-foreground transition-all active:scale-[0.98] disabled:opacity-40 disabled:pointer-events-none"
        >
          <Github className="h-5 w-5" />
          Continue with GitHub
        </button>
      </div>

      {/* Divider */}
      <div className="relative flex items-center gap-3">
        <div className="flex-1 h-px bg-border/30" />
        <span className="text-[11px] font-semibold text-muted-foreground/40 uppercase tracking-widest">or</span>
        <div className="flex-1 h-px bg-border/30" />
      </div>

      {/* Form */}
      <form
        id="sign-up-form"
        onSubmit={(e) => {
          e.preventDefault();
          if (!isPending) form.handleSubmit();
        }}
        className="space-y-4"
        noValidate
      >
        {/* Full name */}
        <form.Field name="name">
          {(field) => {
            const invalid = field.state.meta.isTouched && field.state.meta.errors.length > 0;
            return (
              <div className="space-y-1.5">
                <label htmlFor="name" className="block text-xs font-semibold text-foreground/70 tracking-wide">
                  Full name
                </label>
                <input
                  id="name"
                  name="name"
                  type="text"
                  value={field.state.value}
                  onBlur={field.handleBlur}
                  onChange={(e) => field.handleChange(e.target.value)}
                  placeholder="John Doe"
                  autoComplete="name"
                  disabled={isPending}
                  required
                  className={cn(
                    "w-full h-11 px-4 rounded-xl border bg-secondary/30 text-sm text-foreground placeholder:text-muted-foreground/40 outline-none transition-all shadow-inner",
                    "focus:ring-2 focus:ring-primary/30 focus:border-primary/60 focus:bg-background",
                    "disabled:opacity-50 disabled:cursor-not-allowed",
                    invalid ? "border-destructive/60" : "border-border/60 hover:border-border"
                  )}
                />
                {invalid && (
                  <p className="text-[11px] text-destructive font-medium">{field.state.meta.errors[0]}</p>
                )}
              </div>
            );
          }}
        </form.Field>

        {/* Email */}
        <form.Field name="email">
          {(field) => {
            const invalid = field.state.meta.isTouched && field.state.meta.errors.length > 0;
            return (
              <div className="space-y-1.5">
                <label htmlFor="email" className="block text-xs font-semibold text-foreground/70 tracking-wide">
                  Email
                </label>
                <input
                  id="email"
                  name="email"
                  type="email"
                  value={field.state.value}
                  onBlur={field.handleBlur}
                  onChange={(e) => field.handleChange(e.target.value)}
                  placeholder="name@example.com"
                  autoComplete="email"
                  disabled={isPending}
                  required
                  className={cn(
                    "w-full h-11 px-4 rounded-xl border bg-secondary/30 text-sm text-foreground placeholder:text-muted-foreground/40 outline-none transition-all shadow-inner",
                    "focus:ring-2 focus:ring-primary/30 focus:border-primary/60 focus:bg-background",
                    "disabled:opacity-50 disabled:cursor-not-allowed",
                    invalid ? "border-destructive/60" : "border-border/60 hover:border-border"
                  )}
                />
                {invalid && (
                  <p className="text-[11px] text-destructive font-medium">{field.state.meta.errors[0]}</p>
                )}
              </div>
            );
          }}
        </form.Field>

        {/* Password */}
        <form.Field name="password">
          {(field) => {
            const invalid = field.state.meta.isTouched && field.state.meta.errors.length > 0;
            return (
              <div className="space-y-1.5">
                <label htmlFor="password" className="block text-xs font-semibold text-foreground/70 tracking-wide">
                  Password
                </label>
                <div className="relative">
                  <input
                    id="password"
                    name="password"
                    type={showPassword ? "text" : "password"}
                    value={field.state.value}
                    onBlur={field.handleBlur}
                    onChange={(e) => {
                      field.handleChange(e.target.value);
                      setPasswordValue(e.target.value);
                    }}
                    autoComplete="new-password"
                    disabled={isPending}
                    required
                    className={cn(
                      "w-full h-11 px-4 pr-10 rounded-xl border bg-secondary/30 text-sm text-foreground placeholder:text-muted-foreground/40 outline-none transition-all shadow-inner",
                      "focus:ring-2 focus:ring-primary/30 focus:border-primary/60 focus:bg-background",
                      "disabled:opacity-50 disabled:cursor-not-allowed",
                      invalid ? "border-destructive/60" : "border-border/60 hover:border-border"
                    )}
                  />
                  <button
                    type="button"
                    tabIndex={-1}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground/40 hover:text-muted-foreground transition-colors"
                    onClick={() => setShowPassword((v) => !v)}
                  >
                    {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                </div>
                {invalid && (
                  <p className="text-[11px] text-destructive font-medium">{field.state.meta.errors[0]}</p>
                )}

                {/* Password strength hints — show while typing */}
                {passwordValue.length > 0 && (
                  <div className="flex flex-col gap-1 pt-1">
                    {PASSWORD_RULES.map((rule) => {
                      const passes = rule.test(passwordValue);
                      return (
                        <div key={rule.label} className={cn("flex items-center gap-1.5 text-[10px] font-medium transition-colors", passes ? "text-emerald-400/80" : "text-muted-foreground/35")}>
                          <Check className={cn("h-3 w-3 shrink-0 transition-opacity", passes ? "opacity-100" : "opacity-20")} />
                          {rule.label}
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
            );
          }}
        </form.Field>

        {/* Submit */}
        <button
          type="submit"
          form="sign-up-form"
          disabled={isPending}
          className={cn(
            "w-full h-11 rounded-xl font-bold text-sm flex items-center justify-center gap-2 transition-all active:scale-[0.98]",
            "bg-primary text-primary-foreground hover:bg-primary/90 shadow-lg shadow-primary/25",
            "disabled:opacity-60 disabled:cursor-not-allowed disabled:active:scale-100"
          )}
        >
          {isPending ? (
            <>
              <Loader2 className="h-4 w-4 animate-spin" />
              Creating account…
            </>
          ) : (
            <>
              Create account
              <ArrowRight className="h-4 w-4" />
            </>
          )}
        </button>
      </form>

      {/* Footer */}
      <p className="text-center text-xs text-muted-foreground/50">
        Already have an account?{" "}
        <Link
          to="/auth/sign-in"
          className="font-semibold text-foreground/70 hover:text-foreground transition-colors underline underline-offset-2"
        >
          Sign in
        </Link>
      </p>
    </div>
  );
}
