import { useState } from "react";
import { useForm } from "@tanstack/react-form";
import { toast } from "sonner";
import { Eye, EyeOff, Loader2, Check } from "lucide-react";
import { cn } from "@/lib/utils";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useGetMeQuery, useUpdateProfile, useChangePassword } from "@/features/auth/auth-api";

const PASSWORD_RULES = [
  { label: "At least 8 characters", test: (v: string) => v.length >= 8 },
  { label: "One uppercase letter", test: (v: string) => /[A-Z]/.test(v) },
  { label: "One number", test: (v: string) => /[0-9]/.test(v) },
];

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

function UpdateProfileForm() {
  const { data: user } = useGetMeQuery();
  const { mutate: updateProfile } = useUpdateProfile();
  const [isPending, setIsPending] = useState(false);

  const form = useForm({
    defaultValues: { name: user?.name ?? "", email: user?.email ?? "" },
    onSubmit: async ({ value }) => {
      setIsPending(true);
      try {
        await updateProfile(value);
        toast.success("Profile updated");
      } catch (err: unknown) {
        const e = err as { data?: { detail?: string }; message?: string };
        toast.error(e.data?.detail || e.message || "Failed to update profile");
      } finally {
        setIsPending(false);
      }
    },
  });

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        if (!isPending) form.handleSubmit();
      }}
      className="space-y-3"
      noValidate
    >
      <form.Field name="name">
        {(field) => (
          <div className="space-y-1">
            <label className="block text-[11px] font-semibold text-foreground/60 tracking-wide">
              Full name
            </label>
            <input
              type="text"
              value={field.state.value}
              onBlur={field.handleBlur}
              onChange={(e) => field.handleChange(e.target.value)}
              autoComplete="name"
              disabled={isPending}
              className="w-full h-8 px-3 rounded-md border border-border/40 bg-card/30 text-xs text-foreground outline-none transition-all focus:ring-1 focus:ring-primary/50 focus:border-primary/50 disabled:opacity-50"
            />
          </div>
        )}
      </form.Field>

      <form.Field name="email">
        {(field) => (
          <div className="space-y-1">
            <label className="block text-[11px] font-semibold text-foreground/60 tracking-wide">
              Email
            </label>
            <input
              type="email"
              value={field.state.value}
              onBlur={field.handleBlur}
              onChange={(e) => field.handleChange(e.target.value)}
              autoComplete="email"
              disabled={isPending}
              className="w-full h-8 px-3 rounded-md border border-border/40 bg-card/30 text-xs text-foreground outline-none transition-all focus:ring-1 focus:ring-primary/50 focus:border-primary/50 disabled:opacity-50"
            />
          </div>
        )}
      </form.Field>

      <div className="flex justify-end">
        <button
          type="submit"
          disabled={isPending}
          className="h-7 px-3 rounded-md bg-primary text-primary-foreground text-xs font-semibold flex items-center gap-1.5 hover:bg-primary/90 disabled:opacity-60 transition-colors"
        >
          {isPending && <Loader2 className="h-3 w-3 animate-spin" />}
          Save changes
        </button>
      </div>
    </form>
  );
}

function ChangePasswordForm() {
  const { mutate: changePassword } = useChangePassword();
  const [isPending, setIsPending] = useState(false);
  const [showCurrent, setShowCurrent] = useState(false);
  const [showNew, setShowNew] = useState(false);
  const [newPasswordValue, setNewPasswordValue] = useState("");

  const form = useForm({
    defaultValues: { currentPassword: "", newPassword: "", confirmPassword: "" },
    onSubmit: async ({ value }) => {
      if (value.newPassword !== value.confirmPassword) {
        toast.error("New passwords don't match");
        return;
      }
      setIsPending(true);
      try {
        await changePassword({ currentPassword: value.currentPassword, newPassword: value.newPassword });
        toast.success("Password changed");
        form.reset();
        setNewPasswordValue("");
      } catch (err: unknown) {
        const e = err as { data?: { detail?: string }; message?: string };
        toast.error(e.data?.detail || e.message || "Failed to change password");
      } finally {
        setIsPending(false);
      }
    },
  });

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        if (!isPending) form.handleSubmit();
      }}
      className="space-y-3"
      noValidate
    >
      <form.Field name="currentPassword">
        {(field) => (
          <div className="space-y-1">
            <label className="block text-[11px] font-semibold text-foreground/60 tracking-wide">
              Current password
            </label>
            <div className="relative">
              <input
                type={showCurrent ? "text" : "password"}
                value={field.state.value}
                onBlur={field.handleBlur}
                onChange={(e) => field.handleChange(e.target.value)}
                autoComplete="current-password"
                disabled={isPending}
                className="w-full h-8 px-3 pr-9 rounded-md border border-border/40 bg-card/30 text-xs text-foreground outline-none transition-all focus:ring-1 focus:ring-primary/50 focus:border-primary/50 disabled:opacity-50"
              />
              <button
                type="button"
                tabIndex={-1}
                onClick={() => setShowCurrent((v) => !v)}
                className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground/40 hover:text-muted-foreground transition-colors"
              >
                {showCurrent ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
              </button>
            </div>
          </div>
        )}
      </form.Field>

      <form.Field name="newPassword">
        {(field) => (
          <div className="space-y-1">
            <label className="block text-[11px] font-semibold text-foreground/60 tracking-wide">
              New password
            </label>
            <div className="relative">
              <input
                type={showNew ? "text" : "password"}
                value={field.state.value}
                onBlur={field.handleBlur}
                onChange={(e) => {
                  field.handleChange(e.target.value);
                  setNewPasswordValue(e.target.value);
                }}
                autoComplete="new-password"
                disabled={isPending}
                className="w-full h-8 px-3 pr-9 rounded-md border border-border/40 bg-card/30 text-xs text-foreground outline-none transition-all focus:ring-1 focus:ring-primary/50 focus:border-primary/50 disabled:opacity-50"
              />
              <button
                type="button"
                tabIndex={-1}
                onClick={() => setShowNew((v) => !v)}
                className="absolute right-2.5 top-1/2 -translate-y-1/2 text-muted-foreground/40 hover:text-muted-foreground transition-colors"
              >
                {showNew ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
              </button>
            </div>
            {newPasswordValue.length > 0 && (
              <div className="flex flex-col gap-0.5 pt-0.5">
                {PASSWORD_RULES.map((rule) => {
                  const passes = rule.test(newPasswordValue);
                  return (
                    <div
                      key={rule.label}
                      className={cn(
                        "flex items-center gap-1.5 text-[10px] font-medium transition-colors",
                        passes ? "text-emerald-400/80" : "text-muted-foreground/35"
                      )}
                    >
                      <Check className={cn("h-2.5 w-2.5 shrink-0", passes ? "opacity-100" : "opacity-20")} />
                      {rule.label}
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        )}
      </form.Field>

      <form.Field name="confirmPassword">
        {(field) => {
          const mismatch =
            field.state.meta.isTouched &&
            field.state.value.length > 0 &&
            field.state.value !== newPasswordValue;
          return (
            <div className="space-y-1">
              <label className="block text-[11px] font-semibold text-foreground/60 tracking-wide">
                Confirm new password
              </label>
              <input
                type="password"
                value={field.state.value}
                onBlur={field.handleBlur}
                onChange={(e) => field.handleChange(e.target.value)}
                autoComplete="new-password"
                disabled={isPending}
                className={cn(
                  "w-full h-8 px-3 rounded-md border bg-card/30 text-xs text-foreground outline-none transition-all focus:ring-1 focus:ring-primary/50 focus:border-primary/50 disabled:opacity-50",
                  mismatch ? "border-destructive/60" : "border-border/40"
                )}
              />
              {mismatch && (
                <p className="text-[10px] text-destructive font-medium">Passwords don't match</p>
              )}
            </div>
          );
        }}
      </form.Field>

      <div className="flex justify-end">
        <button
          type="submit"
          disabled={isPending}
          className="h-7 px-3 rounded-md bg-primary text-primary-foreground text-xs font-semibold flex items-center gap-1.5 hover:bg-primary/90 disabled:opacity-60 transition-colors"
        >
          {isPending && <Loader2 className="h-3 w-3 animate-spin" />}
          Change password
        </button>
      </div>
    </form>
  );
}

export function ProfileModal({ open, onOpenChange }: Props) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md p-0 overflow-hidden">
        <DialogHeader className="px-5 pt-5 pb-4 border-b border-border/30">
          <DialogTitle className="text-sm font-bold">Account settings</DialogTitle>
        </DialogHeader>

        <div className="overflow-y-auto max-h-[70vh]">
          {/* Profile section */}
          <div className="px-5 py-4">
            <p className="text-[11px] font-bold text-muted-foreground/50 uppercase tracking-widest mb-3">
              Profile
            </p>
            <UpdateProfileForm />
          </div>

          <div className="mx-5 border-t border-border/20" />

          {/* Password section */}
          <div className="px-5 py-4">
            <p className="text-[11px] font-bold text-muted-foreground/50 uppercase tracking-widest mb-3">
              Password
            </p>
            <ChangePasswordForm />
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
