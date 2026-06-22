import { useState } from "react";
import { toast } from "sonner";
import { useForm } from "@tanstack/react-form";
import { useLogin } from "../../auth-api";
import { useNavigate, Link } from "@tanstack/react-router";
import { Eye, EyeOff, Loader2, Github, Chrome, ArrowRight } from "lucide-react";
import { cn } from "@/lib/utils";

export default function SignInForm() {
  const { mutate: login, isPending } = useLogin();
  const navigate = useNavigate();
  const [showPassword, setShowPassword] = useState(false);

  const form = useForm({
    defaultValues: { email: "", password: "" },
    onSubmit: async ({ value }) => {
      try {
        await login(value);
        toast.success("Welcome back!");
        localStorage.removeItem("lastWorkspaceId");
        navigate({ to: "/" });
      } catch (err: unknown) {
        const e = err as { data?: { detail?: string }; message?: string };
        toast.error(e.data?.detail || e.message || "Failed to sign in");
      }
    },
  });

  return (
    <div className="w-full space-y-6">
      {/* OAuth */}
      <div className="grid grid-cols-2 gap-3">
        <button
          type="button"
          disabled={isPending}
          onClick={() => (window.location.href = "/api/auth/external-login/google")}
          className="flex items-center justify-center gap-2 h-10 px-4 rounded-lg border border-border/40 bg-card/40 hover:bg-card/80 text-sm font-medium text-foreground/80 hover:text-foreground transition-all disabled:opacity-40 disabled:pointer-events-none"
        >
          <Chrome className="h-4 w-4" />
          Google
        </button>
        <button
          type="button"
          disabled={isPending}
          onClick={() => (window.location.href = "/api/auth/external-login/github")}
          className="flex items-center justify-center gap-2 h-10 px-4 rounded-lg border border-border/40 bg-card/40 hover:bg-card/80 text-sm font-medium text-foreground/80 hover:text-foreground transition-all disabled:opacity-40 disabled:pointer-events-none"
        >
          <Github className="h-4 w-4" />
          GitHub
        </button>
      </div>

      {/* Divider */}
      <div className="relative flex items-center gap-3">
        <div className="flex-1 h-px bg-border/30" />
        <span className="text-[11px] font-semibold text-muted-foreground/40 uppercase tracking-widest">
          or
        </span>
        <div className="flex-1 h-px bg-border/30" />
      </div>

      {/* Form */}
      <form
        id="sign-in-form"
        onSubmit={(e) => {
          e.preventDefault();
          if (!isPending) form.handleSubmit();
        }}
        className="space-y-4"
        noValidate
      >
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
                    "w-full h-10 px-3.5 rounded-lg border bg-card/30 text-sm text-foreground placeholder:text-muted-foreground/30 outline-none transition-all",
                    "focus:ring-1 focus:ring-primary/50 focus:border-primary/50",
                    "disabled:opacity-50 disabled:cursor-not-allowed",
                    invalid ? "border-destructive/60" : "border-border/40 hover:border-border/70"
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
                <div className="flex items-center justify-between">
                  <label htmlFor="password" className="block text-xs font-semibold text-foreground/70 tracking-wide">
                    Password
                  </label>
                  <Link
                    to="/auth/forgot-password"
                    className="text-[11px] text-muted-foreground/50 hover:text-foreground transition-colors"
                  >
                    Forgot password?
                  </Link>
                </div>
                <div className="relative">
                  <input
                    id="password"
                    name="password"
                    type={showPassword ? "text" : "password"}
                    value={field.state.value}
                    onBlur={field.handleBlur}
                    onChange={(e) => field.handleChange(e.target.value)}
                    autoComplete="current-password"
                    disabled={isPending}
                    required
                    className={cn(
                      "w-full h-10 px-3.5 pr-10 rounded-lg border bg-card/30 text-sm text-foreground placeholder:text-muted-foreground/30 outline-none transition-all",
                      "focus:ring-1 focus:ring-primary/50 focus:border-primary/50",
                      "disabled:opacity-50 disabled:cursor-not-allowed",
                      invalid ? "border-destructive/60" : "border-border/40 hover:border-border/70"
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
              </div>
            );
          }}
        </form.Field>

        {/* Submit */}
        <button
          type="submit"
          form="sign-in-form"
          disabled={isPending}
          className={cn(
            "w-full h-10 rounded-lg font-semibold text-sm flex items-center justify-center gap-2 transition-all active:scale-[0.98]",
            "bg-primary text-primary-foreground hover:bg-primary/90 shadow-lg shadow-primary/20",
            "disabled:opacity-60 disabled:cursor-not-allowed disabled:active:scale-100"
          )}
        >
          {isPending ? (
            <>
              <Loader2 className="h-4 w-4 animate-spin" />
              Signing in…
            </>
          ) : (
            <>
              Sign in
              <ArrowRight className="h-4 w-4" />
            </>
          )}
        </button>
      </form>

      {/* Footer link */}
      <p className="text-center text-xs text-muted-foreground/50">
        Don&apos;t have an account?{" "}
        <Link
          to="/auth/sign-up"
          className="font-semibold text-foreground/70 hover:text-foreground transition-colors underline underline-offset-2"
        >
          Sign up
        </Link>
      </p>
    </div>
  );
}
