import { useState } from "react";
import { useForm } from "@tanstack/react-form";
import { useNavigate, Link } from "@tanstack/react-router";
import { Eye, EyeOff, Loader2, ArrowRight, Check } from "lucide-react";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import { useResetPassword } from "../../auth-api";

const PASSWORD_RULES = [
  { label: "At least 8 characters", test: (v: string) => v.length >= 8 },
  { label: "One uppercase letter", test: (v: string) => /[A-Z]/.test(v) },
  { label: "One number", test: (v: string) => /[0-9]/.test(v) },
];

interface Props {
  token: string;
}

export function ResetPasswordForm({ token }: Props) {
  const { mutate, isPending } = useResetPassword();
  const navigate = useNavigate();
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [passwordValue, setPasswordValue] = useState("");

  const form = useForm({
    defaultValues: { newPassword: "", confirmPassword: "" },
    onSubmit: async ({ value }) => {
      if (value.newPassword !== value.confirmPassword) {
        toast.error("Passwords don't match");
        return;
      }
      try {
        await mutate(token, value.newPassword);
        toast.success("Password updated! Please sign in.");
        navigate({ to: "/auth/sign-in" });
      } catch (err: unknown) {
        const e = err as { data?: { detail?: string }; message?: string };
        toast.error(e.data?.detail || e.message || "Failed to reset password");
      }
    },
  });

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        if (!isPending) form.handleSubmit();
      }}
      className="space-y-4"
      noValidate
    >
      <form.Field name="newPassword">
        {(field) => {
          const invalid = field.state.meta.isTouched && field.state.meta.errors.length > 0;
          return (
            <div className="space-y-1.5">
              <label htmlFor="newPassword" className="block text-xs font-semibold text-foreground/70 tracking-wide">
                New password
              </label>
              <div className="relative">
                <input
                  id="newPassword"
                  name="newPassword"
                  type={showPassword ? "text" : "password"}
                  value={field.state.value}
                  onBlur={field.handleBlur}
                  onChange={(e) => {
                    field.handleChange(e.target.value);
                    setPasswordValue(e.target.value);
                  }}
                  autoComplete="new-password"
                  autoFocus
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
              {passwordValue.length > 0 && (
                <div className="flex flex-col gap-1 pt-1">
                  {PASSWORD_RULES.map((rule) => {
                    const passes = rule.test(passwordValue);
                    return (
                      <div
                        key={rule.label}
                        className={cn(
                          "flex items-center gap-1.5 text-[10px] font-medium transition-colors",
                          passes ? "text-emerald-400/80" : "text-muted-foreground/35"
                        )}
                      >
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

      <form.Field name="confirmPassword">
        {(field) => {
          const invalid = field.state.meta.isTouched && field.state.meta.errors.length > 0;
          const mismatch =
            field.state.meta.isTouched &&
            field.state.value.length > 0 &&
            field.state.value !== passwordValue;
          return (
            <div className="space-y-1.5">
              <label htmlFor="confirmPassword" className="block text-xs font-semibold text-foreground/70 tracking-wide">
                Confirm password
              </label>
              <div className="relative">
                <input
                  id="confirmPassword"
                  name="confirmPassword"
                  type={showConfirm ? "text" : "password"}
                  value={field.state.value}
                  onBlur={field.handleBlur}
                  onChange={(e) => field.handleChange(e.target.value)}
                  autoComplete="new-password"
                  disabled={isPending}
                  required
                  className={cn(
                    "w-full h-10 px-3.5 pr-10 rounded-lg border bg-card/30 text-sm text-foreground placeholder:text-muted-foreground/30 outline-none transition-all",
                    "focus:ring-1 focus:ring-primary/50 focus:border-primary/50",
                    "disabled:opacity-50 disabled:cursor-not-allowed",
                    invalid || mismatch ? "border-destructive/60" : "border-border/40 hover:border-border/70"
                  )}
                />
                <button
                  type="button"
                  tabIndex={-1}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground/40 hover:text-muted-foreground transition-colors"
                  onClick={() => setShowConfirm((v) => !v)}
                >
                  {showConfirm ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {(invalid || mismatch) && (
                <p className="text-[11px] text-destructive font-medium">
                  {mismatch ? "Passwords don't match" : String(field.state.meta.errors[0])}
                </p>
              )}
            </div>
          );
        }}
      </form.Field>

      <button
        type="submit"
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
            Updating…
          </>
        ) : (
          <>
            Set new password
            <ArrowRight className="h-4 w-4" />
          </>
        )}
      </button>

      <p className="text-center text-xs text-muted-foreground/50">
        Remember it?{" "}
        <Link
          to="/auth/sign-in"
          className="font-semibold text-foreground/70 hover:text-foreground transition-colors underline underline-offset-2"
        >
          Sign in
        </Link>
      </p>
    </form>
  );
}
