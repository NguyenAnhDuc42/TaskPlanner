import { useAuth } from "@/features/auth/auth-context";
import { LogOut, User } from "lucide-react";
import { Button } from "@/components/ui/button";

export function Head() {
  const { user, logout } = useAuth();

  if (!user) return null;

  return (
    <div className="flex items-center justify-between px-6 py-4 bg-background border-b border-border">
      <div className="flex items-center gap-3">
        <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center border border-primary/20">
          <User className="h-5 w-5 text-primary" />
        </div>
        <div>
          <h1 className="text-sm font-semibold text-foreground leading-none">
            {user.name}
          </h1>
          <p className="text-xs text-muted-foreground mt-1">{user.email}</p>
        </div>
      </div>

      <Button
        variant="ghost"
        size="sm"
        onClick={logout}
        className="flex items-center gap-2 text-muted-foreground hover:text-destructive hover:bg-destructive/10 transition-colors font-mono"
      >
        <LogOut className="h-4 w-4" />
        Logout
      </Button>
    </div>
  );
}
