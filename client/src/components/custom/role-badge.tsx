import { Badge } from "@/components/ui/badge"
import { mapRoleToBadge, Role } from "@/utils/role-utils"
import { cn } from "@/lib/utils"

interface RoleBadgeProps {
  role: Role | null
  size?: "xs" | "sm" | "md" | "lg"
  className?: string
}

export function RoleBadge({ role, size = "md", className }: RoleBadgeProps) {
  const { roleName, badgeClasses } = mapRoleToBadge(role)
  
  const sizeClasses = {
    xs: "text-[0.65rem] px-1.5 py-0.5",
    sm: "text-xs px-2 py-0.5",
    md: "text-sm px-2.5 py-1",
    lg: "text-base px-3 py-1.5",
  }

  return (
    <Badge 
      className={cn(
        badgeClasses, 
        sizeClasses[size], 
        "font-medium border-transparent",
        className
      )}
    >
      {roleName}
    </Badge>
  )
}