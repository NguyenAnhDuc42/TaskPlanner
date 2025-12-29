import { ComponentExample } from "@/components/component-example";
import { userQueryOptions } from "@/features/auth/api";
import { createFileRoute, redirect } from "@tanstack/react-router";
import { useAuth } from "@/features/auth/auth-context";
import { Button } from "@/components/ui/button";

export const Route = createFileRoute("/")({
  beforeLoad: async ({ context }) => {
    const user = await context.queryClient.ensureQueryData(userQueryOptions);
    if (!user) {
      throw redirect({ to: "/auth/sign-in" });
    }
  },
  component: Index,
});

function Index() {
  const { user, logout } = useAuth();

  return (
    <div className="p-4 space-y-6">
      <div className="flex justify-between items-center bg-card p-4 rounded-lg border shadow-sm">
        <div>
          <h3 className="text-lg font-semibold">
            Welcome back, {user?.name}!
          </h3>
          <p className="text-sm text-muted-foreground">{user?.email}</p>
        </div>
        <Button variant="destructive" onClick={() => logout()}>
          Log out
        </Button>
      </div>

      <div className="pt-4">
        <ComponentExample />
      </div>
    </div>
  );
}
