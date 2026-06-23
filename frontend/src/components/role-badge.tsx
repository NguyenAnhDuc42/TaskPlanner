import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { Role } from "@/types/role";

export function RoleBadge({ role, className }: { role: Role, className?: string }) {
  if (role === "None") return null;

  let variant: "default" | "secondary" | "outline" | "destructive" = "outline";
  let badgeClass = "text-[10px] px-1.5 h-4 font-mono uppercase tracking-wider";

  switch (role) {
    case "Owner":
      variant = "default";
      badgeClass = cn(
        badgeClass,
        "bg-red-500/20 text-red-500 border-red-500/30 hover:bg-red-500/40 border",
      );
      break;
    case "Admin":
      variant = "outline";
      badgeClass = cn(
        badgeClass,
        "bg-blue-500/10 text-blue-500 border-blue-500/30 hover:bg-blue-500/20 border",
      );
      break;
    case "Member":
      variant = "secondary";
      badgeClass = cn(
        badgeClass,
        "bg-green-500/10 text-green-500 border-green-500/30 border",
      );
      break;
    case "Guest":
      variant = "secondary";
      badgeClass = cn(badgeClass, "opacity-60 border border-gray-500/30");
      break;
  }

  return (
    <Badge variant={variant} className={cn(badgeClass, className)} style={{ borderRadius: "0.25rem" }}>
      {role}
    </Badge>
  );
}
