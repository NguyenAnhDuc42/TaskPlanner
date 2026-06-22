import { useState } from "react";
import { useForm } from "@tanstack/react-form";
import { Link } from "@tanstack/react-router";
import { ArrowLeft, ArrowRight, Loader2, MailCheck } from "lucide-react";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import { useForgotPassword } from "../../auth-api";

export function ForgotPasswordForm() {
  const { mutate, isPending } = useForgotPassword();
  const [sent, setSent] = useState(false);
  const [submittedEmail, setSubmittedEmail] = useState("");

  const form = useForm({
    defaultValues: { email: "" },
    onSubmit: async ({ value }) => {
      try {
        await mutate(value.email);
        setSubmittedEmail(value.email);
        setSent(true);
      } catch (err: unknown) {
        const e = err as { data?: { detail?: string }; message?: string };
        toast.error(e.data?.detail || e.message || "Something went wrong");
      }
    },
  });

  if (sent) {
    return (
      <div className="space-y-6 text-center">
        <div className="flex justify-center">
          <div className="h-14 w-14 rounded-full bg-primary/10 flex items-center justify-center">
            <MailCheck className="h-7 w-7 text-primary" />
          </div>
        </div>
        <div>
          <p className="text-sm text-muted-foreground/70 leading-relaxed">
            We sent a reset link to <span className="font-semibold text-foreground/80">{submittedEmail}</span>.
            Check your inbox and follow the link to set a new password.
          </p>
          <p className="mt-2 text-xs text-muted-foreground/40">The link expires in 1 hour.</p>
        </div>
        <Link
          to="/auth/sign-in"
          className="inline-flex items-center gap-1.5 text-xs font-semibold text-foreground/70 hover:text-foreground transition-colors"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          Back to sign in
        </Link>
      </div>
    );
  }

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        if (!isPending) form.handleSubmit();
      }}
      className="space-y-4"
      noValidate
    >
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
                autoFocus
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
            Sending…
          </>
        ) : (
          <>
            Send reset link
            <ArrowRight className="h-4 w-4" />
          </>
        )}
      </button>

      <p className="text-center text-xs text-muted-foreground/50">
        <Link
          to="/auth/sign-in"
          className="inline-flex items-center gap-1 font-semibold text-foreground/70 hover:text-foreground transition-colors"
        >
          <ArrowLeft className="h-3 w-3" />
          Back to sign in
        </Link>
      </p>
    </form>
  );
}
